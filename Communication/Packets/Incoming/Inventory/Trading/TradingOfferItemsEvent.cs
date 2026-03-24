using Plus.Communication.Packets.Outgoing.Inventory.Trading;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Inventory.Trading;

internal class TradingOfferItemsEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (!habbo.InRoom)
            return Task.CompletedTask;
        var room = habbo.CurrentRoom;
        if (room == null)
            return Task.CompletedTask;
        var roomUser = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (roomUser == null)
            return Task.CompletedTask;
        var amount = packet.ReadInt();
        var itemId = packet.ReadUInt();
        if (!roomUser.IsTrading)
        {
            session.Send(new TradingClosedComposer(habbo.Id));
            return Task.CompletedTask;
        }
        if (!room.GetTrading().TryGetTrade(roomUser.TradeId, out var trade))
        {
            session.Send(new TradingClosedComposer(habbo.Id));
            return Task.CompletedTask;
        }
        var furniture = habbo.Inventory?.Furniture;
        var item = furniture?.GetItem(itemId);
        if (item == null)
            return Task.CompletedTask;
        if (!trade.CanChange)
            return Task.CompletedTask;
        var tradeUser = trade.Users[0];
        if (tradeUser.RoomUser != roomUser)
            tradeUser = trade.Users[1];
        var allItems = furniture.AllItems.Where(x => x.Definition.Id == item.Definition.Id).Take(amount).ToList();
        foreach (var I in allItems)
        {
            if (tradeUser.OfferedItems.ContainsKey(I.Id))
                return Task.CompletedTask;
            trade.RemoveAccepted();
            tradeUser.OfferedItems.Add(I.Id, I);
        }
        trade.SendPacket(new TradingUpdateComposer(trade));
        return Task.CompletedTask;
    }
}
