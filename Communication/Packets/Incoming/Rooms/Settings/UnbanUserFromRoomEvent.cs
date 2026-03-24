using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Settings;

internal class UnbanUserFromRoomEvent : IPacketEvent
{
    private readonly IRoomAccessService _roomAccessService;

    public UnbanUserFromRoomEvent(IRoomAccessService roomAccessService)
    {
        _roomAccessService = roomAccessService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var userId = packet.ReadInt();
        var roomId = packet.ReadInt();
        return _roomAccessService.UnbanUser(session, userId, roomId);
    }
}
