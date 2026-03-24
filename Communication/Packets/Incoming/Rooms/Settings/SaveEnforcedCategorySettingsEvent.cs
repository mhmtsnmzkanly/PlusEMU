using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Settings;

internal class SaveEnforcedCategorySettingsEvent : IPacketEvent
{
    private readonly IRoomAccessService _roomAccessService;

    public SaveEnforcedCategorySettingsEvent(IRoomAccessService roomAccessService)
    {
        _roomAccessService = roomAccessService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var roomId = packet.ReadUInt();
        var categoryId = packet.ReadInt();
        var tradeSettings = packet.ReadInt();
        return _roomAccessService.SaveEnforcedCategorySettings(session, roomId, categoryId, tradeSettings);
    }
}
