using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModerateRoomEvent : IPacketEvent
{
    private readonly IModerationRoomService _moderationRoomService;

    public ModerateRoomEvent(IModerationRoomService moderationRoomService)
    {
        _moderationRoomService = moderationRoomService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var roomId = packet.ReadUInt();
        var setLock = packet.ReadInt() == 1;
        var setName = packet.ReadInt() == 1;
        var kickAll = packet.ReadInt() == 1;
        return _moderationRoomService.ModerateRoom(session, roomId, setLock, setName, kickAll);
    }
}
