using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class AssignRightsEvent : RoomPacketEvent
{
    private readonly IRoomAccessService _roomAccessService;

    public AssignRightsEvent(IRoomAccessService roomAccessService)
    {
        _roomAccessService = roomAccessService;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet) => _roomAccessService.AssignRights(room, session, packet.ReadInt());
}
