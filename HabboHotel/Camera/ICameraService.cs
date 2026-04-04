using Plus.HabboHotel.GameClients;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Camera;

[Scoped]
public interface ICameraService
{
    Task SendConfiguration(GameClient session);
    Task RenderRoom(GameClient session, bool thumbnail);
    Task PurchasePhoto(GameClient session);
    Task PublishPhoto(GameClient session);
}
