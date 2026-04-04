using Plus.HabboHotel.GameClients;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Catalog;

[Scoped]
public interface ITargetedOfferService
{
    Task SendCurrentOffer(GameClient session);
    Task Purchase(GameClient session, int offerId, int amount);
    Task SetState(GameClient session, int offerId, int state);
    Task MarkViewed(GameClient session, int? offerId = null);
}
