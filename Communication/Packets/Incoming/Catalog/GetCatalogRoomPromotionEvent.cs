using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Catalog;

internal class GetCatalogRoomPromotionEvent : IPacketEvent
{
    private readonly IRoomFactory _roomFactory;

    public GetCatalogRoomPromotionEvent(IRoomFactory roomFactory)
    {
        _roomFactory = roomFactory;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo)
            return Task.CompletedTask;

        var rooms = _roomFactory.GetRoomsDataByOwnerSortByName(habbo.Id);
        session.Send(new GetCatalogRoomPromotionComposer(rooms));
        return Task.CompletedTask;
    }
}
