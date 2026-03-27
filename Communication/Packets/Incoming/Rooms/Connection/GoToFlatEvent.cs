using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Connection;

internal class GoToFlatEvent : IPacketEvent
{
    private readonly IRoomService _roomService;

    public GoToFlatEvent(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        return _roomService.EnterRoom(session);
    }
}
