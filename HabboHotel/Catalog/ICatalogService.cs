using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Catalog;

public interface ICatalogService
{
    Task PurchaseItem(GameClient session, int pageId, int itemId, string extraData, int amount);
    Task RedeemVoucher(GameClient session, string code);
}
