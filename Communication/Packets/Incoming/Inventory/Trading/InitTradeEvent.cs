using Plus.Communication.Packets.Outgoing.Inventory.Trading;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.Utilities;
using Dapper;

namespace Plus.Communication.Packets.Incoming.Inventory.Trading;

internal class InitTradeEvent : IPacketEvent
{
    private readonly IDatabase _database;

    public InitTradeEvent(IDatabase database)
    {
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var userId = packet.ReadInt();
        if (!habbo.InRoom)
            return Task.CompletedTask;
        var room = habbo.CurrentRoom;
        if (room == null)
            return Task.CompletedTask;
        
        var roomUser = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (roomUser == null)
            return Task.CompletedTask;
        var targetUser = room.GetRoomUserManager().GetRoomUserByVirtualId(userId);
        if (targetUser == null)
            return Task.CompletedTask;
        if (habbo.TradingLockExpiry > 0)
        {
            if (habbo.TradingLockExpiry > UnixTimestamp.GetNow())
            {
                session.SendNotification("You're currently banned from trading.");
                return Task.CompletedTask;
            }
            habbo.TradingLockExpiry = 0;
            session.SendNotification("Your trading ban has now expired.");
            using var connection = _database.Connection();
            connection.Execute("UPDATE `user_info` SET `trading_locked` = '0' WHERE `id` = @userId LIMIT 1", new { userId = habbo.Id });
        }
        if (!(habbo.Permissions?.HasRight("room_trade_override") ?? false))
        {
            if (room.TradeSettings == 0)
            {
                session.Send(new TradingErrorComposer(6, targetUser.GetUsername()));
                return Task.CompletedTask;
            }
            if (room.TradeSettings == 1 && room.OwnerId != habbo.Id)
            {
                session.Send(new TradingErrorComposer(6, targetUser.GetUsername()));
                return Task.CompletedTask;
            }
        }
        if (roomUser.IsTrading && roomUser.TradePartner != targetUser.UserId)
        {
            session.Send(new TradingErrorComposer(7, targetUser.GetUsername()));
            return Task.CompletedTask;
        }
        if (targetUser.IsTrading && targetUser.TradePartner != roomUser.UserId)
        {
            session.Send(new TradingErrorComposer(8, targetUser.GetUsername()));
            return Task.CompletedTask;
        }
        var targetClient = targetUser.GetClient();
        var targetHabbo = targetClient?.GetHabbo();
        if (targetHabbo == null)
        {
            session.Send(new TradingErrorComposer(4, targetUser.GetUsername()));
            return Task.CompletedTask;
        }
        if (!targetHabbo.AllowTradingRequests)
        {
            session.Send(new TradingErrorComposer(4, targetUser.GetUsername()));
            return Task.CompletedTask;
        }
        if (targetHabbo.TradingLockExpiry > 0)
        {
            session.Send(new TradingErrorComposer(4, targetUser.GetUsername()));
            return Task.CompletedTask;
        }
        if (!room.GetTrading().StartTrade(roomUser, targetUser, out var trade))
        {
            session.SendNotification("An error occured trying to start this trade");
            return Task.CompletedTask;
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
        return Task.CompletedTask;
    }
}
