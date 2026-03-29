namespace Plus.HabboHotel.Catalog;

public interface ICatalogManager
{
    Dictionary<int, int> ItemOffers { get; }
    Task Init();
    bool TryGetBot(uint itemId, out CatalogBot bot);
    bool TryGetPage(int pageId, out CatalogPage page);
    bool TryGetDeal(int dealId, out CatalogDeal deal);
    ICollection<CatalogPage> Pages { get; }
    ICollection<CatalogPromotion> Promotions { get; }
}
