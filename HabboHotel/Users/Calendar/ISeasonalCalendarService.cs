using Plus.HabboHotel.GameClients;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Users.Calendar;

[Scoped]
public interface ISeasonalCalendarService
{
    Task SendCalendarData(GameClient session);
    Task OpenDoor(GameClient session, string campaignName, int day, bool force);
}
