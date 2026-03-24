using Plus.Communication.Packets;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Communication.Packets.Outgoing.Inventory.Trading;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms.Trading;

public sealed class Trade
{
    private readonly Room _instance;

    public Trade(int id, RoomUser playerOne, RoomUser playerTwo, Room room)
    {
        Id = id;
        CanChange = true;
        _instance = room;
        Users = new TradeUser[2];
        Users[0] = new(playerOne);
        Users[1] = new(playerTwo);
        playerOne.IsTrading = true;
        playerOne.TradeId = Id;
        playerOne.TradePartner = playerTwo.UserId;
        playerTwo.IsTrading = true;
        playerTwo.TradeId = Id;
        playerTwo.TradePartner = playerOne.UserId;
    }

    public int Id { get; set; }
    public TradeUser[] Users { get; set; }
    public bool CanChange { get; set; }

    public bool AllAccepted
    {
        get
        {
            foreach (var user in Users)
            {
                if (user == null)
                    continue;
                if (!user.HasAccepted) return false;
            }
            return true;
        }
    }

    public void SendPacket(IServerPacket packet)
    {
        foreach (var user in Users)
        {
            if (user == null || user.RoomUser == null || user.RoomUser.GetClient() == null)
                continue;
            user.RoomUser.GetClient().Send(packet);
        }
    }

    public void RemoveAccepted()
    {
        foreach (var user in Users)
        {
            if (user == null)
                continue;
            user.HasAccepted = false;
        }
    }

    public void EndTrade(int userId)
    {
        foreach (var tradeUser in Users)
        {
            if (tradeUser == null || tradeUser.RoomUser == null)
                continue;
            RemoveTrade(tradeUser.RoomUser.UserId);
        }
        SendPacket(new TradingClosedComposer(userId));
        _instance.GetTrading().RemoveTrade(Id);
    }

    public void Finish()
    {
        foreach (var tradeUser in Users)
        {
            if (tradeUser == null)
                continue;
            RemoveTrade(tradeUser.RoomUser.UserId);
        }
        ProcessItems();
        SendPacket(new TradingFinishComposer());
        _instance.GetTrading().RemoveTrade(Id);
    }

    public void RemoveTrade(int userId)
    {
        var tradeUser = Users[0];
        if (tradeUser.RoomUser.UserId != userId) tradeUser = Users[1];
        tradeUser.RoomUser.RemoveStatus("trd");
        tradeUser.RoomUser.UpdateNeeded = true;
        tradeUser.RoomUser.IsTrading = false;
        tradeUser.RoomUser.TradeId = 0;
        tradeUser.RoomUser.TradePartner = 0;
    }

    public void ProcessItems()
    {
        var userOne = Users[0].OfferedItems.Values.ToList();
        var userTwo = Users[1].OfferedItems.Values.ToList();
        var roomUserOne = Users[0].RoomUser;
        var roomUserTwo = Users[1].RoomUser;
        var clientOne = roomUserOne?.GetClient();
        var clientTwo = roomUserTwo?.GetClient();
        var habboOne = clientOne?.GetHabbo();
        var habboTwo = clientTwo?.GetHabbo();
        var inventoryOne = habboOne?.Inventory?.Furniture;
        var inventoryTwo = habboTwo?.Inventory?.Furniture;
        var logUserOne = "";
        var logUserTwo = "";
        if (roomUserOne == null || clientOne == null || habboOne == null || inventoryOne == null)
            return;
        if (roomUserTwo == null || clientTwo == null || habboTwo == null || inventoryTwo == null)
            return;
        foreach (var item in userOne)
        {
            var I = inventoryOne.GetItem(item.Id);
            if (I == null)
            {
                SendPacket(new BroadcastMessageAlertComposer("Error! Trading Failed!"));
                return;
            }
        }
        foreach (var item in userTwo)
        {
            var I = inventoryTwo.GetItem(item.Id);
            if (I == null)
            {
                SendPacket(new BroadcastMessageAlertComposer("Error! Trading Failed!"));
                return;
            }
        }
        using var dbClient = PlusEnvironment.DatabaseManager.GetQueryReactor();
        foreach (var item in userOne)
        {
            logUserOne += $"{item.Id};";
            inventoryOne.RemoveItem(item.Id);
            clientOne.Send(new FurniListRemoveComposer(item.Id));
            if (item.Definition.InteractionType == InteractionType.Exchange && PlusEnvironment.SettingsManager.TryGetValue("trading.auto_exchange_redeemables") == "1")
            {
                habboTwo.Credits += item.Definition.BehaviourData;
                clientTwo.Send(new CreditBalanceComposer(habboTwo.Credits));
                dbClient.SetQuery("DELETE FROM `items` WHERE `id` = @id LIMIT 1");
                dbClient.AddParameter("id", item.Id);
                dbClient.RunQuery();
            }
            else
            {
                if (inventoryTwo.AddItem(item))
                {
                    clientTwo.Send(new FurniListAddComposer(item));
                    clientTwo.Send(new FurniListNotificationComposer(item.Id, 1));
                    dbClient.SetQuery("UPDATE `items` SET `user_id` = @user WHERE id=@id LIMIT 1");
                    dbClient.AddParameter("user", roomUserTwo.UserId);
                    dbClient.AddParameter("id", item.Id);
                    dbClient.RunQuery();
                }
            }
        }
        foreach (var item in userTwo)
        {
            logUserTwo += $"{item.Id};";
            inventoryTwo.RemoveItem(item.Id);
            clientTwo.Send(new FurniListRemoveComposer(item.Id));
            if (item.Definition.InteractionType == InteractionType.Exchange && PlusEnvironment.SettingsManager.TryGetValue("trading.auto_exchange_redeemables") == "1")
            {
                habboOne.Credits += item.Definition.BehaviourData;
                clientOne.Send(new CreditBalanceComposer(habboOne.Credits));
                dbClient.SetQuery("DELETE FROM `items` WHERE `id` = @id LIMIT 1");
                dbClient.AddParameter("id", item.Id);
                dbClient.RunQuery();
            }
            else
            {
                if (inventoryOne.AddItem(item))
                {
                    clientOne.Send(new FurniListAddComposer(item));
                    clientOne.Send(new FurniListNotificationComposer(item.Id, 1));
                    dbClient.SetQuery("UPDATE `items` SET `user_id` = @user WHERE id=@id LIMIT 1");
                    dbClient.AddParameter("user", roomUserOne.UserId);
                    dbClient.AddParameter("id", item.Id);
                    dbClient.RunQuery();
                }
            }
        }
        dbClient.SetQuery("INSERT INTO `logs_client_trade` VALUES(null, @1id, @2id, @1items, @2items, UNIX_TIMESTAMP())");
        dbClient.AddParameter("1id", roomUserOne.UserId);
        dbClient.AddParameter("2id", roomUserTwo.UserId);
        dbClient.AddParameter("1items", logUserOne);
        dbClient.AddParameter("2items", logUserTwo);
        dbClient.RunQuery();
    }
}
