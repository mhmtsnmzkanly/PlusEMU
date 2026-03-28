using Plus.Communication.Packets.Incoming.Rooms;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Navigator;

internal class GoToHotelViewEvent : RoomPacketEvent
{
    private readonly IRoomService _roomService;

    public GoToHotelViewEvent(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet) => _roomService.LeaveRoom(session);
}
