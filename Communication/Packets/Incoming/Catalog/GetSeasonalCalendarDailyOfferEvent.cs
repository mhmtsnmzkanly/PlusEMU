using Plus.HabboHotel.Users.Calendar;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Catalog;

internal class GetSeasonalCalendarDailyOfferEvent : IPacketEvent
{
    private readonly ISeasonalCalendarService _seasonalCalendarService;

    public GetSeasonalCalendarDailyOfferEvent(ISeasonalCalendarService seasonalCalendarService) => _seasonalCalendarService = seasonalCalendarService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _seasonalCalendarService.SendCalendarData(session);
}
