using Plus.HabboHotel.GameClients;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Guides;

[Singleton]
public interface IGuideService
{
    Task SendToolState(GameClient session, bool onDutyOverride = false, bool useOverride = false);
    Task ConfigureDuty(GameClient session, bool onDuty, bool helperRequests, bool bullyReports);
    Task RequestAssistance(GameClient session, int requestType, string message);
    Task HandleRequest(GameClient session, bool accepted);
    Task SendSessionMessage(GameClient session, string message);
    Task SendRequesterRoom(GameClient session);
    Task InviteRequesterToRoom(GameClient session);
    Task CancelRequest(GameClient session);
    Task CloseRequest(GameClient session);
    Task ReportPartner(GameClient session, string message);
}
