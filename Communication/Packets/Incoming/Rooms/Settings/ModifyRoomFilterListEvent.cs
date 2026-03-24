using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Settings;

internal class ModifyRoomFilterListEvent : IPacketEvent
{
    private readonly IRoomAccessService _roomAccessService;

    public ModifyRoomFilterListEvent(IRoomAccessService roomAccessService)
    {
        _roomAccessService = roomAccessService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        packet.ReadInt(); //roomId
        var added = packet.ReadBool();
        var word = packet.ReadString();
        return _roomAccessService.ModifyRoomFilterList(session, added, word);
    }
}
