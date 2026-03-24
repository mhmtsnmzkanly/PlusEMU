using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Catalog.Marketplace;

public interface IMarketplaceService
{
    Task MakeOffer(GameClient session, int sellingPrice, uint itemId);
    Task BuyOffer(GameClient session, int offerId);
    Task GetOffers(GameClient session, int minCost, int maxCost, string searchQuery, int filterMode);
    Task GetOwnOffers(GameClient session);
    Task GetCanMakeOffer(GameClient session);
    Task RedeemOfferCredits(GameClient session);
    Task CancelOffer(GameClient session, uint offerId);
}
