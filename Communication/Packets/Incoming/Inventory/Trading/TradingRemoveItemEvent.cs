using Plus.Communication.Packets.Outgoing.Inventory.Trading;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Inventory.Trading;

internal class TradingRemoveItemEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var furniture = habbo?.Inventory?.Furniture;
        if (habbo == null || furniture == null || !habbo.InRoom)
            return Task.CompletedTask;
        var room = habbo.CurrentRoom;
        if (room == null)
            return Task.CompletedTask;
        var roomUser = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (roomUser == null)
            return Task.CompletedTask;
        var itemId = packet.ReadUInt();
        if (!room.GetTrading().TryGetTrade(roomUser.TradeId, out var trade) || trade == null)
        {
            session.Send(new TradingClosedComposer(habbo.Id));
            return Task.CompletedTask;
        }
        var item = furniture.GetItem(itemId);
        if (item == null)
            return Task.CompletedTask;
        if (!trade.CanChange)
            return Task.CompletedTask;
        var user = trade.Users[0];
        if (user.RoomUser != roomUser)
            user = trade.Users[1];
        if (!user.OfferedItems.ContainsKey(item.Id))
            return Task.CompletedTask;
        trade.RemoveAccepted();
        user.OfferedItems.Remove(item.Id);
        trade.SendPacket(new TradingUpdateComposer(trade));
        return Task.CompletedTask;
    }
}
