using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class LetUserInEvent : RoomPacketEvent
{
    private readonly IRoomAccessService _roomAccessService;

    public LetUserInEvent(IRoomAccessService roomAccessService)
    {
        _roomAccessService = roomAccessService;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var name = packet.ReadString();
        var accepted = packet.ReadBool();
        return _roomAccessService.LetUserIn(room, session, name, accepted);
    }
}
