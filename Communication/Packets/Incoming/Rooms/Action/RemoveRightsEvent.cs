using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class RemoveRightsEvent : RoomPacketEvent
{
    private readonly IRoomAccessService _roomAccessService;

    public RemoveRightsEvent(IRoomAccessService roomAccessService)
    {
        _roomAccessService = roomAccessService;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var amount = packet.ReadInt();
        var userIds = new List<int>(amount);
        for (var i = 0; i < amount; i++)
            userIds.Add(packet.ReadInt());
        return _roomAccessService.RemoveRights(room, session, userIds);
    }
}
