using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Communication.Packets.Outgoing.Inventory.Trading;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Core.Language;
using Plus.Core.Settings;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Plus.Utilities;

namespace Plus.HabboHotel.Rooms.Trading;

internal class TradingService : ITradingService
{
    private readonly IDatabase _database;
    private readonly ILanguageManager _languageManager;
    private readonly ISettingsManager _settingsManager;

    public TradingService(IDatabase database, ILanguageManager languageManager, ISettingsManager settingsManager)
    {
        _database = database;
        _languageManager = languageManager;
        _settingsManager = settingsManager;
    }

    public async Task StartTrade(GameClient session, int targetVirtualId)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.TryGetCurrentRoom(out var room))
            return;

        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var roomUser) || roomUser == null)
            return;

        if (!room.GetRoomUserManager().TryGetRoomUserByVirtualId(targetVirtualId, out var targetUser) || targetUser == null)
            return;

        if (habbo.TradingLockExpiry > 0)
        {
            if (habbo.TradingLockExpiry > UnixTimestamp.GetNow())
            {
                session.SendNotification(_languageManager.Require("trading.locked"));
                return;
            }

            habbo.TradingLockExpiry = 0;
            session.SendNotification(_languageManager.Require("trading.lock_expired"));
            using var connection = _database.Connection();
            await connection.ExecuteAsync(
                "UPDATE `user_info` SET `trading_locked` = '0' WHERE `id` = @userId LIMIT 1",
                new { userId = habbo.Id });
        }

        if (!(habbo.Permissions?.HasRight("room_trade_override") ?? false))
        {
            if (room.TradeSettings == 0)
            {
                session.Send(new TradingErrorComposer(6, targetUser.GetUsername()));
                return;
            }

            if (room.TradeSettings == 1 && room.OwnerId != habbo.Id)
            {
                session.Send(new TradingErrorComposer(6, targetUser.GetUsername()));
                return;
            }
        }

        if (roomUser.IsTrading && roomUser.TradePartner != targetUser.UserId)
        {
            session.Send(new TradingErrorComposer(7, targetUser.GetUsername()));
            return;
        }

        if (targetUser.IsTrading && targetUser.TradePartner != roomUser.UserId)
        {
            session.Send(new TradingErrorComposer(8, targetUser.GetUsername()));
            return;
        }

        var targetClient = targetUser.GetClient();
        var targetHabbo = targetClient?.GetHabbo();
        if (targetHabbo == null)
        {
            session.Send(new TradingErrorComposer(4, targetUser.GetUsername()));
            return;
        }

        if (!targetHabbo.AllowTradingRequests || targetHabbo.TradingLockExpiry > 0)
        {
            session.Send(new TradingErrorComposer(4, targetUser.GetUsername()));
            return;
        }

        if (!room.GetTrading().StartTrade(roomUser, targetUser, out var trade))
        {
            session.SendNotification(_languageManager.Require("trading.start.error"));
            return;
        }

        if (targetUser.HasStatus("trd"))
            targetUser.RemoveStatus("trd");
        if (roomUser.HasStatus("trd"))
            roomUser.RemoveStatus("trd");

        targetUser.SetStatus("trd");
        targetUser.UpdateNeeded = true;
        roomUser.SetStatus("trd");
        roomUser.UpdateNeeded = true;

        trade.SendPacket(new TradingStartComposer(roomUser.UserId, targetUser.UserId));
    }

    public Task OfferItem(GameClient session, uint itemId)
    {
        if (!TryGetTradeContext(session, requireTradingFlag: true, out var habbo, out _, out var roomUser, out var trade))
            return Task.CompletedTask;

        var item = habbo.Inventory?.Furniture?.GetItem(itemId);
        if (item == null || !trade.CanChange)
            return Task.CompletedTask;

        var tradeUser = GetTradeUser(trade, roomUser);
        if (tradeUser == null || tradeUser.OfferedItems.ContainsKey(item.Id))
            return Task.CompletedTask;

        trade.RemoveAccepted();
        if (tradeUser.OfferedItems.Count <= 499)
        {
            var totalLimiteds = tradeUser.OfferedItems.Count(x => x.Value.UniqueNumber > 0);
            if (totalLimiteds < 9)
                tradeUser.OfferedItems.Add(item.Id, item);
        }

        trade.SendPacket(new TradingUpdateComposer(trade));
        return Task.CompletedTask;
    }

    public Task OfferItems(GameClient session, uint itemId, int amount)
    {
        if (!TryGetTradeContext(session, requireTradingFlag: true, out var habbo, out _, out var roomUser, out var trade))
            return Task.CompletedTask;

        var furniture = habbo.Inventory?.Furniture;
        var item = furniture?.GetItem(itemId);
        if (furniture == null || item == null || !trade.CanChange)
            return Task.CompletedTask;

        var tradeUser = GetTradeUser(trade, roomUser);
        if (tradeUser == null)
            return Task.CompletedTask;

        var allItems = furniture.AllItems
            .Where(x => x.Definition.Id == item.Definition.Id)
            .Take(amount)
            .ToList();

        foreach (var inventoryItem in allItems)
        {
            if (tradeUser.OfferedItems.ContainsKey(inventoryItem.Id))
                return Task.CompletedTask;

            trade.RemoveAccepted();
            tradeUser.OfferedItems.Add(inventoryItem.Id, inventoryItem);
        }

        trade.SendPacket(new TradingUpdateComposer(trade));
        return Task.CompletedTask;
    }

    public Task RemoveItem(GameClient session, uint itemId)
    {
        if (!TryGetTradeContext(session, requireTradingFlag: false, out var habbo, out _, out var roomUser, out var trade))
            return Task.CompletedTask;

        var item = habbo.Inventory?.Furniture?.GetItem(itemId);
        if (item == null || !trade.CanChange)
            return Task.CompletedTask;

        var tradeUser = GetTradeUser(trade, roomUser);
        if (tradeUser == null || !tradeUser.OfferedItems.ContainsKey(item.Id))
            return Task.CompletedTask;

        trade.RemoveAccepted();
        tradeUser.OfferedItems.Remove(item.Id);
        trade.SendPacket(new TradingUpdateComposer(trade));
        return Task.CompletedTask;
    }

    public Task Accept(GameClient session)
    {
        if (!TryGetTradeContext(session, requireTradingFlag: false, out var habbo, out var room, out var roomUser, out var trade))
            return Task.CompletedTask;

        var tradeUser = GetTradeUser(trade, roomUser);
        if (tradeUser == null)
            return Task.CompletedTask;

        tradeUser.HasAccepted = true;
        trade.SendPacket(new TradingAcceptComposer(habbo.Id, true));
        if (trade.AllAccepted)
        {
            trade.SendPacket(new TradingCompleteComposer());
            trade.CanChange = false;
            trade.RemoveAccepted();
        }

        return Task.CompletedTask;
    }

    public async Task Confirm(GameClient session)
    {
        if (!TryGetTradeContext(session, requireTradingFlag: false, out var habbo, out var room, out var roomUser, out var trade))
            return;

        if (trade.CanChange)
            return;

        var tradeUser = GetTradeUser(trade, roomUser);
        if (tradeUser == null)
            return;

        tradeUser.HasAccepted = true;
        trade.SendPacket(new TradingConfirmedComposer(habbo.Id, true));
        if (!trade.AllAccepted)
            return;

        ResetTradeUsers(trade);
        await ProcessItems(trade);
        trade.SendPacket(new TradingFinishComposer());
        room.GetTrading().RemoveTrade(trade.Id);
    }

    public Task Cancel(GameClient session)
    {
        if (!TryGetTradeContext(session, requireTradingFlag: false, out var habbo, out _, out _, out var trade))
            return Task.CompletedTask;

        trade.EndTrade(habbo.Id);
        return Task.CompletedTask;
    }

    public Task CancelConfirm(GameClient session)
    {
        if (!TryGetTradeContext(session, requireTradingFlag: false, out var habbo, out _, out _, out var trade))
            return Task.CompletedTask;

        trade.EndTrade(habbo.Id);
        return Task.CompletedTask;
    }

    public Task Modify(GameClient session)
    {
        if (!TryGetTradeContext(session, requireTradingFlag: false, out var habbo, out _, out var roomUser, out var trade))
            return Task.CompletedTask;

        if (!trade.CanChange)
            return Task.CompletedTask;

        var tradeUser = GetTradeUser(trade, roomUser);
        if (tradeUser == null)
            return Task.CompletedTask;

        tradeUser.HasAccepted = false;
        trade.SendPacket(new TradingAcceptComposer(habbo.Id, false));
        return Task.CompletedTask;
    }

    private static TradeUser? GetTradeUser(Trade trade, RoomUser roomUser)
    {
        if (trade.Users[0].RoomUser == roomUser)
            return trade.Users[0];
        if (trade.Users[1].RoomUser == roomUser)
            return trade.Users[1];
        return null;
    }

    private static void ResetTradeUsers(Trade trade)
    {
        foreach (var tradeUser in trade.Users)
        {
            if (tradeUser?.RoomUser == null)
                continue;
            trade.RemoveTrade(tradeUser.RoomUser.UserId);
        }
    }

    private bool TryGetTradeContext(
        GameClient session,
        bool requireTradingFlag,
        out Habbo habbo,
        out Room room,
        out RoomUser roomUser,
        out Trade trade)
    {
        habbo = null!;
        room = null!;
        roomUser = null!;
        trade = null!;

        var sessionHabbo = session.GetHabbo();
        if (sessionHabbo == null || !sessionHabbo.TryGetCurrentRoom(out var currentRoom))
            return SendClosedIfPossible(session, sessionHabbo);

        if (!currentRoom.GetRoomUserManager().TryGetRoomUserByHabbo(sessionHabbo.Id, out var currentRoomUser) || currentRoomUser == null)
            return SendClosedIfPossible(session, sessionHabbo);

        if (requireTradingFlag && !currentRoomUser.IsTrading)
            return SendClosedIfPossible(session, sessionHabbo);

        if (!currentRoom.GetTrading().TryGetTrade(currentRoomUser.TradeId, out var currentTrade) || currentTrade == null)
            return SendClosedIfPossible(session, sessionHabbo);

        habbo = sessionHabbo;
        room = currentRoom;
        roomUser = currentRoomUser;
        trade = currentTrade;
        return true;
    }

    private static bool SendClosedIfPossible(GameClient session, Habbo? habbo)
    {
        if (habbo != null)
            session.Send(new TradingClosedComposer(habbo.Id));
        return false;
    }

    private async Task ProcessItems(Trade trade)
    {
        var userOneItems = trade.Users[0].OfferedItems.Values.ToList();
        var userTwoItems = trade.Users[1].OfferedItems.Values.ToList();
        var roomUserOne = trade.Users[0].RoomUser;
        var roomUserTwo = trade.Users[1].RoomUser;
        var clientOne = roomUserOne?.GetClient();
        var clientTwo = roomUserTwo?.GetClient();
        var habboOne = clientOne?.GetHabbo();
        var habboTwo = clientTwo?.GetHabbo();
        var inventoryOne = habboOne?.Inventory?.Furniture;
        var inventoryTwo = habboTwo?.Inventory?.Furniture;

        if (roomUserOne == null || roomUserTwo == null || clientOne == null || clientTwo == null || habboOne == null || habboTwo == null ||
            inventoryOne == null || inventoryTwo == null)
            return;

        foreach (var item in userOneItems)
        {
            if (inventoryOne.GetItem(item.Id) == null)
            {
                trade.SendPacket(new BroadcastMessageAlertComposer(_languageManager.Require("trading.failed")));
                return;
            }
        }

        foreach (var item in userTwoItems)
        {
            if (inventoryTwo.GetItem(item.Id) == null)
            {
                trade.SendPacket(new BroadcastMessageAlertComposer(_languageManager.Require("trading.failed")));
                return;
            }
        }

        var autoRedeemExchangeables = _settingsManager.GetBoolOrDefault("trading.auto_exchange_redeemables", false);
        var logUserOne = string.Empty;
        var logUserTwo = string.Empty;

        using var connection = _database.Connection();

        foreach (var item in userOneItems)
        {
            logUserOne += $"{item.Id};";
            inventoryOne.RemoveItem(item.Id);
            clientOne.Send(new FurniListRemoveComposer(item.Id));

            if (item.Definition.IsExchange && autoRedeemExchangeables)
            {
                habboTwo.Credits += item.Definition.BehaviourData;
                clientTwo.Send(new CreditBalanceComposer(habboTwo.Credits));
                await connection.ExecuteAsync("DELETE FROM `items` WHERE `id` = @id LIMIT 1", new { id = item.Id });
                continue;
            }

            if (!inventoryTwo.AddItem(item))
                continue;

            clientTwo.Send(new FurniListAddComposer(item));
            clientTwo.Send(new FurniListNotificationComposer(item.Id, 1));
            await connection.ExecuteAsync(
                "UPDATE `items` SET `user_id` = @userId WHERE `id` = @id LIMIT 1",
                new { userId = roomUserTwo.UserId, id = item.Id });
        }

        foreach (var item in userTwoItems)
        {
            logUserTwo += $"{item.Id};";
            inventoryTwo.RemoveItem(item.Id);
            clientTwo.Send(new FurniListRemoveComposer(item.Id));

            if (item.Definition.IsExchange && autoRedeemExchangeables)
            {
                habboOne.Credits += item.Definition.BehaviourData;
                clientOne.Send(new CreditBalanceComposer(habboOne.Credits));
                await connection.ExecuteAsync("DELETE FROM `items` WHERE `id` = @id LIMIT 1", new { id = item.Id });
                continue;
            }

            if (!inventoryOne.AddItem(item))
                continue;

            clientOne.Send(new FurniListAddComposer(item));
            clientOne.Send(new FurniListNotificationComposer(item.Id, 1));
            await connection.ExecuteAsync(
                "UPDATE `items` SET `user_id` = @userId WHERE `id` = @id LIMIT 1",
                new { userId = roomUserOne.UserId, id = item.Id });
        }

        await connection.ExecuteAsync(
            "INSERT INTO `logs_client_trade` VALUES (NULL, @userOneId, @userTwoId, @userOneItems, @userTwoItems, UNIX_TIMESTAMP())",
            new
            {
                userOneId = roomUserOne.UserId,
                userTwoId = roomUserTwo.UserId,
                userOneItems = logUserOne,
                userTwoItems = logUserTwo
            });
    }
}
