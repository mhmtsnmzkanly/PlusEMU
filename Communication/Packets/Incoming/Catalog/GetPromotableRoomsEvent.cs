using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.Catalog;

internal class GetPromotableRoomsEvent : IPacketEvent
{
    private readonly IRoomFactory _roomFactory;

    public GetPromotableRoomsEvent(IRoomFactory roomFactory)
    {
        _roomFactory = roomFactory;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo)
            return Task.CompletedTask;

        var rooms = _roomFactory.GetRoomsDataByOwnerSortByName(habbo.Id);
        rooms = rooms.Where(x => x.Promotion == null || x.Promotion.TimestampExpires < UnixTimestamp.GetNow()).ToList();
        session.Send(new PromotableRoomsComposer(rooms));
        return Task.CompletedTask;
    }
}
