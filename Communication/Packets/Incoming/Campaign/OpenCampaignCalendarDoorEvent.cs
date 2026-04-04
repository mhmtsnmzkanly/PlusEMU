using Plus.HabboHotel.Users.Calendar;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Campaign;

internal class OpenCampaignCalendarDoorEvent : IPacketEvent
{
    private readonly ISeasonalCalendarService _seasonalCalendarService;

    public OpenCampaignCalendarDoorEvent(ISeasonalCalendarService seasonalCalendarService) => _seasonalCalendarService = seasonalCalendarService;

    public Task Parse(GameClient session, IIncomingPacket packet) =>
        _seasonalCalendarService.OpenDoor(session, packet.ReadString(), packet.ReadInt(), false);
}
