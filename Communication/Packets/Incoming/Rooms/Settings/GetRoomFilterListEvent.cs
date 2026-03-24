using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Settings;

internal class GetRoomFilterListEvent : IPacketEvent
{
    private readonly IRoomAccessService _roomAccessService;

    public GetRoomFilterListEvent(IRoomAccessService roomAccessService)
    {
        _roomAccessService = roomAccessService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _roomAccessService.GetRoomFilterList(session);
}
