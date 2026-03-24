using System.Data;
using System.Text;
using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Communication.Packets.Outgoing.Marketplace;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Plus.Utilities;

namespace Plus.HabboHotel.Catalog.Marketplace;

internal class MarketplaceService : IMarketplaceService
{
    private readonly IMarketplaceManager _marketplaceManager;
    private readonly IItemDataManager _itemDataManager;
    private readonly IDatabase _database;
    private readonly IItemFactory _itemFactory;

    public MarketplaceService(
        IMarketplaceManager marketplaceManager,
        IItemDataManager itemDataManager,
        IDatabase database,
        IItemFactory itemFactory)
    {
        _marketplaceManager = marketplaceManager;
        _itemDataManager = itemDataManager;
        _database = database;
        _itemFactory = itemFactory;
    }

    public Task MakeOffer(GameClient session, int sellingPrice, uint itemId)
    {
        var habbo = session.GetHabbo();
        var inventory = habbo?.Inventory;
        if (habbo == null || inventory?.Furniture == null)
        {
            session.Send(new MarketplaceMakeOfferResultComposer(0));
            return Task.CompletedTask;
        }

        var item = inventory.Furniture.GetItem(itemId);
        if (item == null || sellingPrice > 70000000 || sellingPrice == 0)
        {
            session.Send(new MarketplaceMakeOfferResultComposer(0));
            return Task.CompletedTask;
        }

        var definition = item.Definition;
        if (definition == null)
        {
            session.Send(new MarketplaceMakeOfferResultComposer(0));
            return Task.CompletedTask;
        }

        var comission = _marketplaceManager.CalculateComissionPrice(sellingPrice);
        var totalPrice = sellingPrice + comission;
        var itemType = definition.Type == ItemType.Wall ? 2 : 1;

        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery(
                $"INSERT INTO `catalog_marketplace_offers` (`furni_id`,`item_id`,`user_id`,`asking_price`,`total_price`,`public_name`,`sprite_id`,`item_type`,`timestamp`,`extra_data`,`limited_number`,`limited_stack`) VALUES ('{itemId}','{definition.Id}','{habbo.Id}','{sellingPrice}','{totalPrice}',@public_name,'{definition.SpriteId}','{itemType}','{UnixTimestamp.GetNow()}',@extra_data, '{item.UniqueNumber}', '{item.UniqueSeries}')");
            dbClient.AddParameter("public_name", definition.PublicName);
            dbClient.AddParameter("extra_data", item.ExtraData);
            dbClient.RunQuery();
            dbClient.RunQuery($"DELETE FROM `items` WHERE `id` = '{itemId}' AND `user_id` = '{habbo.Id}' LIMIT 1");
        }

        inventory.Furniture.RemoveItem(itemId);
        session.Send(new FurniListRemoveComposer(itemId));
        session.Send(new MarketplaceMakeOfferResultComposer(1));
        return Task.CompletedTask;
    }

    public Task BuyOffer(GameClient session, int offerId)
    {
        var habbo = session.GetHabbo();
        var furniture = habbo?.Inventory?.Furniture;
        if (habbo == null || furniture == null)
            return Task.CompletedTask;

        DataRow? row;
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery(
                "SELECT `state`,`timestamp`,`total_price`,`asking_price`,`extra_data`,`item_id`,`furni_id`,`user_id`,`limited_number`,`limited_stack` FROM `catalog_marketplace_offers` WHERE `offer_id` = @OfferId LIMIT 1");
            dbClient.AddParameter("OfferId", offerId);
            row = dbClient.GetRow();
        }

        if (row == null)
            return ReloadOffers(session, -1, -1, string.Empty, 1);

        if (Convert.ToString(row["state"]) == "2")
        {
            session.SendNotification("Oops, this offer is no longer available.");
            return ReloadOffers(session, -1, -1, string.Empty, 1);
        }

        if (_marketplaceManager.FormatTimestamp() > Convert.ToDouble(row["timestamp"]))
        {
            session.SendNotification("Oops, this offer has expired..");
            return ReloadOffers(session, -1, -1, string.Empty, 1);
        }

        if (!_itemDataManager.Items.TryGetValue(Convert.ToUInt32(row["item_id"]), out var item))
        {
            session.SendNotification("Item isn't in the hotel anymore.");
            return ReloadOffers(session, -1, -1, string.Empty, 1);
        }

        if (Convert.ToInt32(row["user_id"]) == habbo.Id)
        {
            session.SendNotification("To prevent average boosting you cannot purchase your own marketplace offers.");
            return Task.CompletedTask;
        }

        if (Convert.ToInt32(row["total_price"]) > habbo.Credits)
        {
            session.SendNotification("Oops, you do not have enough credits for this.");
            return Task.CompletedTask;
        }

        habbo.Credits -= Convert.ToInt32(row["total_price"]);
        session.Send(new CreditBalanceComposer(habbo.Credits));

        var extraData = Convert.ToString(row["extra_data"]) ?? string.Empty;
        var giveItem = _itemFactory.CreateSingleItem(
            item,
            habbo,
            extraData,
            extraData,
            Convert.ToUInt32(row["furni_id"]),
            Convert.ToUInt32(row["limited_number"]),
            Convert.ToUInt32(row["limited_stack"])).ToInventoryItem();

        if (giveItem != null)
        {
            furniture.AddItem(giveItem);
            session.Send(new FurniListNotificationComposer(giveItem.Id, 1));
            session.Send(new PurchaseOkComposer());
            session.Send(new FurniListAddComposer(giveItem));
            session.Send(new FurniListUpdateComposer());
        }

        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.RunQuery($"UPDATE `catalog_marketplace_offers` SET `state` = '2' WHERE `offer_id` = '{offerId}' LIMIT 1");
            dbClient.SetQuery($"SELECT `id` FROM `catalog_marketplace_data` WHERE `sprite` = {item.SpriteId} LIMIT 1;");
            var id = dbClient.GetInteger();
            if (id > 0)
                dbClient.RunQuery($"UPDATE `catalog_marketplace_data` SET `sold` = `sold` + 1, `avgprice` = (avgprice + {Convert.ToInt32(row["total_price"])}) WHERE `id` = {id} LIMIT 1;");
            else
                dbClient.RunQuery($"INSERT INTO `catalog_marketplace_data` (`sprite`, `sold`, `avgprice`) VALUES ('{item.SpriteId}', '1', '{Convert.ToInt32(row["total_price"])}')");
        }

        if (_marketplaceManager.MarketAverages.ContainsKey(item.SpriteId) &&
            _marketplaceManager.MarketCounts.ContainsKey(item.SpriteId))
        {
            var soldCount = _marketplaceManager.MarketCounts[item.SpriteId];
            var total = _marketplaceManager.MarketAverages[item.SpriteId] + Convert.ToInt32(row["total_price"]);
            _marketplaceManager.MarketAverages[item.SpriteId] = total;
            _marketplaceManager.MarketCounts[item.SpriteId] = soldCount + 1;
        }
        else
        {
            if (!_marketplaceManager.MarketAverages.ContainsKey(item.SpriteId))
                _marketplaceManager.MarketAverages.Add(item.SpriteId, Convert.ToInt32(row["total_price"]));
            if (!_marketplaceManager.MarketCounts.ContainsKey(item.SpriteId))
                _marketplaceManager.MarketCounts.Add(item.SpriteId, 1);
        }

        return ReloadOffers(session, -1, -1, string.Empty, 1);
    }

    public Task GetOffers(GameClient session, int minCost, int maxCost, string searchQuery, int filterMode) =>
        ReloadOffers(session, minCost, maxCost, searchQuery, filterMode);

    public Task GetOwnOffers(GameClient session)
    {
        var habbo = session.GetHabbo();
        if (habbo != null)
            session.Send(new MarketPlaceOwnOffersComposer(habbo.Id));

        return Task.CompletedTask;
    }

    public Task GetCanMakeOffer(GameClient session)
    {
        var habbo = session.GetHabbo();
        var errorCode = habbo?.TradingLockExpiry > 0 ? 6 : 1;
        session.Send(new MarketplaceCanMakeOfferResultComposer(errorCode));
        return Task.CompletedTask;
    }

    public Task RedeemOfferCredits(GameClient session)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var creditsOwed = 0;
        DataTable? table;
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery($"SELECT `asking_price` FROM `catalog_marketplace_offers` WHERE `user_id` = '{habbo.Id}' AND `state` = '2'");
            table = dbClient.GetTable();
        }

        if (table != null)
        {
            foreach (DataRow row in table.Rows)
                creditsOwed += Convert.ToInt32(row["asking_price"]);

            if (creditsOwed >= 1)
            {
                habbo.Credits += creditsOwed;
                session.Send(new CreditBalanceComposer(habbo.Credits));
            }

            using var dbClient = _database.GetQueryReactor();
            dbClient.RunQuery($"DELETE FROM `catalog_marketplace_offers` WHERE `user_id` = '{habbo.Id}' AND `state` = '2'");
        }

        return Task.CompletedTask;
    }

    public async Task CancelOffer(GameClient session, uint offerId)
    {
        var habbo = session.GetHabbo();
        var success = habbo != null && await _marketplaceManager.TryCancelOffer(habbo, offerId);
        session.Send(new MarketplaceCancelOfferResultComposer(offerId, success));
    }

    private Task ReloadOffers(GameClient session, int minCost, int maxCost, string searchQuery, int filterMode)
    {
        DataTable? table;
        var builder = new StringBuilder();
        builder.Append($"WHERE `state` = '1' AND `timestamp` >= {_marketplaceManager.FormatTimestampString()}");
        if (minCost >= 0)
            builder.Append($" AND `total_price` > {minCost}");
        if (maxCost >= 0)
            builder.Append($" AND `total_price` < {maxCost}");
        if (searchQuery.Length >= 1)
            builder.Append(" AND `public_name` LIKE @search_query");

        var ordering = filterMode == 1 ? "ORDER BY `asking_price` DESC" : "ORDER BY `asking_price` ASC";

        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery($"SELECT `offer_id`, `item_type`, `sprite_id`, `total_price`, `limited_number`,`limited_stack` FROM `catalog_marketplace_offers` {builder} {ordering} LIMIT 500");
            dbClient.AddParameter("search_query", $"%{searchQuery}%");
            table = dbClient.GetTable();
        }

        _marketplaceManager.MarketItems.Clear();
        _marketplaceManager.MarketItemKeys.Clear();

        if (table != null)
        {
            foreach (DataRow row in table.Rows)
            {
                var offerId = Convert.ToInt32(row["offer_id"]);
                if (_marketplaceManager.MarketItemKeys.Contains(offerId))
                    continue;

                _marketplaceManager.MarketItemKeys.Add(offerId);
                _marketplaceManager.MarketItems.Add(new(
                    Convert.ToUInt32(row["offer_id"]),
                    Convert.ToUInt32(row["sprite_id"]),
                    Convert.ToInt32(row["total_price"]),
                    int.Parse(row["item_type"].ToString() ?? "0"),
                    Convert.ToUInt32(row["limited_number"]),
                    Convert.ToUInt32(row["limited_stack"])));
            }
        }

        var offers = new Dictionary<uint, MarketOffer>();
        var offerCounts = new Dictionary<uint, int>();
        foreach (var item in _marketplaceManager.MarketItems)
        {
            if (offers.ContainsKey(item.SpriteId))
            {
                if (item.LimitedNumber > 0)
                {
                    if (!offers.ContainsKey(item.OfferId))
                        offers.Add(item.OfferId, item);
                    if (!offerCounts.ContainsKey(item.OfferId))
                        offerCounts.Add(item.OfferId, 1);
                }
                else
                {
                    if (offers[item.SpriteId].TotalPrice > item.TotalPrice)
                        offers[item.SpriteId] = item;
                    offerCounts[item.SpriteId] = offerCounts[item.SpriteId] + 1;
                }
            }
            else
            {
                if (!offers.ContainsKey(item.SpriteId))
                    offers.Add(item.SpriteId, item);
                if (!offerCounts.ContainsKey(item.SpriteId))
                    offerCounts.Add(item.SpriteId, 1);
            }
        }

        session.Send(new MarketPlaceOffersComposer(offers, offerCounts));
        return Task.CompletedTask;
    }
}
