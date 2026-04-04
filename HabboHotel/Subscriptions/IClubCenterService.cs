using Plus.HabboHotel.GameClients;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Subscriptions;

[Scoped]
public interface IClubCenterService
{
    Task SendClubCenterData(GameClient session, int windowId);
    Task SendClubGifts(GameClient session);
}
