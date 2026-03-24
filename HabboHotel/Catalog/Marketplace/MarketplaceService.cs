using System.Data;
using System.Text;
using Dapper;
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
    private sealed class MarketplaceOfferRow
    {
        public string State { get; set; } = string.Empty;
        public double Timestamp { get; set; }
        public int TotalPrice { get; set; }
        public int AskingPrice { get; set; }
        public string ExtraData { get; set; } = string.Empty;
        public uint ItemId { get; set; }
        public uint FurniId { get; set; }
        public int UserId { get; set; }
        public uint LimitedNumber { get; set; }
        public uint LimitedStack { get; set; }
    }

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

        using (var connection = _database.Connection())
        {
            connection.Execute(
                "INSERT INTO `catalog_marketplace_offers` (`furni_id`,`item_id`,`user_id`,`asking_price`,`total_price`,`public_name`,`sprite_id`,`item_type`,`timestamp`,`extra_data`,`limited_number`,`limited_stack`) VALUES (@furniId,@itemDefId,@userId,@sellingPrice,@totalPrice,@publicName,@spriteId,@itemType,@timestamp,@extraData,@limitedNumber,@limitedStack)",
                new
                {
                    furniId = itemId,
                    itemDefId = definition.Id,
                    userId = habbo.Id,
                    sellingPrice,
                    totalPrice,
                    publicName = definition.PublicName,
                    spriteId = definition.SpriteId,
                    itemType,
                    timestamp = UnixTimestamp.GetNow(),
                    extraData = item.ExtraData,
                    limitedNumber = item.UniqueNumber,
                    limitedStack = item.UniqueSeries
                });
            connection.Execute(
                "DELETE FROM `items` WHERE `id` = @itemId AND `user_id` = @userId LIMIT 1",
                new { itemId, userId = habbo.Id });
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

        MarketplaceOfferRow? row;
        using (var connection = _database.Connection())
        {
            row = connection.QuerySingleOrDefault<MarketplaceOfferRow>(
                "SELECT `state` AS State, `timestamp` AS Timestamp, `total_price` AS TotalPrice, `asking_price` AS AskingPrice, `extra_data` AS ExtraData, `item_id` AS ItemId, `furni_id` AS FurniId, `user_id` AS UserId, `limited_number` AS LimitedNumber, `limited_stack` AS LimitedStack FROM `catalog_marketplace_offers` WHERE `offer_id` = @offerId LIMIT 1",
                new { offerId });
        }

        if (row == null)
            return ReloadOffers(session, -1, -1, string.Empty, 1);

        if (row.State == "2")
        {
            session.SendNotification("Oops, this offer is no longer available.");
            return ReloadOffers(session, -1, -1, string.Empty, 1);
        }

        if (_marketplaceManager.FormatTimestamp() > row.Timestamp)
        {
            session.SendNotification("Oops, this offer has expired..");
            return ReloadOffers(session, -1, -1, string.Empty, 1);
        }

        if (!_itemDataManager.Items.TryGetValue(row.ItemId, out var item))
        {
            session.SendNotification("Item isn't in the hotel anymore.");
            return ReloadOffers(session, -1, -1, string.Empty, 1);
        }

        if (row.UserId == habbo.Id)
        {
            session.SendNotification("To prevent average boosting you cannot purchase your own marketplace offers.");
            return Task.CompletedTask;
        }

        if (row.TotalPrice > habbo.Credits)
        {
            session.SendNotification("Oops, you do not have enough credits for this.");
            return Task.CompletedTask;
        }

        habbo.Credits -= row.TotalPrice;
        session.Send(new CreditBalanceComposer(habbo.Credits));

        var extraData = row.ExtraData ?? string.Empty;
        var giveItem = _itemFactory.CreateSingleItem(
            item,
            habbo,
            extraData,
            extraData,
            row.FurniId,
            row.LimitedNumber,
            row.LimitedStack).ToInventoryItem();

        if (giveItem != null)
        {
            furniture.AddItem(giveItem);
            session.Send(new FurniListNotificationComposer(giveItem.Id, 1));
            session.Send(new PurchaseOkComposer());
            session.Send(new FurniListAddComposer(giveItem));
            session.Send(new FurniListUpdateComposer());
        }

        using (var connection = _database.Connection())
        {
            connection.Execute(
                "UPDATE `catalog_marketplace_offers` SET `state` = '2' WHERE `offer_id` = @offerId LIMIT 1",
                new { offerId });
            var dataId = connection.QuerySingleOrDefault<int?>(
                "SELECT `id` FROM `catalog_marketplace_data` WHERE `sprite` = @spriteId LIMIT 1",
                new { spriteId = item.SpriteId });
            if (dataId.GetValueOrDefault() > 0)
            {
                connection.Execute(
                    "UPDATE `catalog_marketplace_data` SET `sold` = `sold` + 1, `avgprice` = (avgprice + @totalPrice) WHERE `id` = @id LIMIT 1",
                    new { totalPrice = row.TotalPrice, id = dataId });
            }
            else
            {
                connection.Execute(
                    "INSERT INTO `catalog_marketplace_data` (`sprite`, `sold`, `avgprice`) VALUES (@spriteId, 1, @totalPrice)",
                    new { spriteId = item.SpriteId, totalPrice = row.TotalPrice });
            }
        }

        if (_marketplaceManager.MarketAverages.ContainsKey(item.SpriteId) &&
            _marketplaceManager.MarketCounts.ContainsKey(item.SpriteId))
        {
            var soldCount = _marketplaceManager.MarketCounts[item.SpriteId];
            var total = _marketplaceManager.MarketAverages[item.SpriteId] + row.TotalPrice;
            _marketplaceManager.MarketAverages[item.SpriteId] = total;
            _marketplaceManager.MarketCounts[item.SpriteId] = soldCount + 1;
        }
        else
        {
            if (!_marketplaceManager.MarketAverages.ContainsKey(item.SpriteId))
                _marketplaceManager.MarketAverages.Add(item.SpriteId, row.TotalPrice);
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

        using (var connection = _database.Connection())
        {
            var creditsOwed = connection.Query<int>(
                "SELECT `asking_price` FROM `catalog_marketplace_offers` WHERE `user_id` = @userId AND `state` = '2'",
                new { userId = habbo.Id }).Sum();

            if (creditsOwed >= 1)
            {
                habbo.Credits += creditsOwed;
                session.Send(new CreditBalanceComposer(habbo.Credits));
            }

            connection.Execute(
                "DELETE FROM `catalog_marketplace_offers` WHERE `user_id` = @userId AND `state` = '2'",
                new { userId = habbo.Id });
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
        var builder = new StringBuilder();
        builder.Append($"WHERE `state` = '1' AND `timestamp` >= {_marketplaceManager.FormatTimestampString()}");
        if (minCost >= 0)
            builder.Append($" AND `total_price` > {minCost}");
        if (maxCost >= 0)
            builder.Append($" AND `total_price` < {maxCost}");
        if (searchQuery.Length >= 1)
            builder.Append(" AND `public_name` LIKE @search_query");

        var ordering = filterMode == 1 ? "ORDER BY `asking_price` DESC" : "ORDER BY `asking_price` ASC";

        using var connection = _database.Connection();
        var table = connection.Query(
            $"SELECT `offer_id` AS OfferId, `item_type` AS ItemType, `sprite_id` AS SpriteId, `total_price` AS TotalPrice, `limited_number` AS LimitedNumber, `limited_stack` AS LimitedStack FROM `catalog_marketplace_offers` {builder} {ordering} LIMIT 500",
            new { search_query = $"%{searchQuery}%" });

        _marketplaceManager.MarketItems.Clear();
        _marketplaceManager.MarketItemKeys.Clear();

        foreach (var row in table)
        {
            var offerId = (int)row.OfferId;
            if (_marketplaceManager.MarketItemKeys.Contains(offerId))
                continue;

            _marketplaceManager.MarketItemKeys.Add(offerId);
            _marketplaceManager.MarketItems.Add(new(
                (uint)row.OfferId,
                (uint)row.SpriteId,
                (int)row.TotalPrice,
                (int)row.ItemType,
                (uint)row.LimitedNumber,
                (uint)row.LimitedStack));
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
