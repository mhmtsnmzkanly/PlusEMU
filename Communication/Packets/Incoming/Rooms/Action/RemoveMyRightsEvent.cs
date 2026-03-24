using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class RemoveMyRightsEvent : RoomPacketEvent
{
    private readonly IRoomAccessService _roomAccessService;

    public RemoveMyRightsEvent(IRoomAccessService roomAccessService)
    {
        _roomAccessService = roomAccessService;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet) => _roomAccessService.RemoveMyRights(room, session);
}
