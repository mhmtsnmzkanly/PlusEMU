using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.Catalog;

internal class GetPromotableRoomsEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var rooms = RoomFactory.GetRoomsDataByOwnerSortByName(habbo.Id);
        rooms = rooms.Where(x => x.Promotion == null || x.Promotion.TimestampExpires < UnixTimestamp.GetNow()).ToList();
        session.Send(new PromotableRoomsComposer(rooms));
        return Task.CompletedTask;
    }
}
