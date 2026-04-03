using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Core;
using Plus.Database;
using Plus.HabboHotel.Catalog.Clothing;
using Plus.HabboHotel.Catalog.Marketplace;
using Plus.HabboHotel.Catalog.Pets;
using Plus.HabboHotel.Catalog.Vouchers;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Catalog;

public class CatalogManager : ICatalogManager, IStartable
{
    private readonly ILogger<CatalogManager> _logger;
    private readonly Dictionary<uint, CatalogBot> _botPresets;
    private readonly Dictionary<int, CatalogDeal> _deals;
    private readonly Dictionary<int, Dictionary<int, CatalogItem>> _items;
    private readonly Dictionary<int, CatalogPage> _pages;
    private readonly Dictionary<int, CatalogPromotion> _promotions;
    private readonly Dictionary<int, int> _itemOffers;

    private readonly IClothingManager _clothingManager;
    private readonly IDatabase _database;
    private readonly IMarketplaceManager _marketplace;
    private readonly IPetRaceManager _petRaceManager;
    private readonly IVoucherManager _voucherManager;
    private readonly IItemDataManager _itemDataManager;

    public CatalogManager(IMarketplaceManager marketplace, IPetRaceManager petRaceManager, IVoucherManager voucherManager, IClothingManager clothingManager, IDatabase database, ILogger<CatalogManager> logger, IItemDataManager itemDataManager)
    {
        _marketplace = marketplace;
        _petRaceManager = petRaceManager;
        _voucherManager = voucherManager;
        _clothingManager = clothingManager;
        _itemDataManager = itemDataManager;
        _database = database;
        _logger = logger;
        _itemOffers = new();
        _pages = new();
        _botPresets = new();
        _items = new();
        _deals = new();
        _promotions = new();
    }

    public Dictionary<int, int> ItemOffers => _itemOffers;

    public async Task Start() => await Init();

    public async Task Init()
    {
        _voucherManager.Init();
        _clothingManager.Init();
        if (_pages.Count > 0)
            _pages.Clear();
        if (_botPresets.Count > 0)
            _botPresets.Clear();
        if (_items.Count > 0)
            _items.Clear();
        if (_deals.Count > 0)
            _deals.Clear();
        if (_promotions.Count > 0)
            _promotions.Clear();

        using var connection = _database.Connection();

        var items = await connection.QueryAsync<CatalogItemRow>("SELECT `id`,`item_id`,`catalog_name`,`cost_credits`,`cost_pixels`,`cost_diamonds`,`amount`,`page_id`,`limited_sells`,`limited_stack`,`offer_active` AS `HaveOfferRaw`,`extradata`,`badge`,`offer_id` AS `OfferId` FROM `catalog_items`");
        foreach (var row in items)
        {
            var item = new CatalogItem
            {
                Id = row.Id,
                ItemId = row.ItemId,
                CatalogName = row.CatalogName,
                CostCredits = row.CostCredits,
                CostPixels = row.CostPixels,
                CostDiamonds = row.CostDiamonds,
                Amount = row.Amount,
                PageId = row.PageId,
                LimitedEditionSells = row.LimitedEditionSells,
                LimitedEditionStack = row.LimitedEditionStack,
                HaveOffer = ParseBooleanish(row.HaveOfferRaw),
                ExtraData = row.ExtraData,
                Badge = row.Badge,
                OfferId = row.OfferId
            };

            if (item.Amount <= 0)
                continue;

            if (!_itemDataManager.Items.TryGetValue(item.ItemId, out ItemDefinition? definition))
            {
                _logger.LogError("Couldn't load Catalog Item " + item.ItemId + ", no furniture record found.");
                continue;
            }

            if (!_items.ContainsKey(item.PageId))
                _items[item.PageId] = new();

            if (item.OfferId != -1 && !_itemOffers.ContainsKey(item.OfferId))
                _itemOffers.Add(item.OfferId, item.PageId);

            item.Definition = definition;
            _items[item.PageId].Add(item.Id, item);
        }

        var deals = await connection.QueryAsync<CatalogDeal>("SELECT `id`, `items`, `name`, `room_id` FROM `catalog_deals`");
        foreach (CatalogDeal deal in deals)
        {
            if (_deals.ContainsKey(deal.Id))
                continue;

            var itemDataList = new List<CatalogItem>();
            if (!string.IsNullOrWhiteSpace(deal.Items))
            {
                var splitItems = deal.Items.Split(';');
                foreach (var split in splitItems)
                {
                    var item = split.Split('*');
                    if (!uint.TryParse(item[0], out var itemId) || !int.TryParse(item[1], out var amount))
                        continue;

                    if (!_itemDataManager.Items.TryGetValue(itemId, out var data))
                        continue;

                    itemDataList.Add(new()
                    {
                        Id = 0,
                        ItemId = itemId,
                        Definition = data,
                        CatalogName = string.Empty,
                        PageId = 0,
                        CostCredits = 0,
                        CostPixels = 0,
                        CostDiamonds = 0,
                        Amount = amount,
                        LimitedEditionSells = 0,
                        LimitedEditionStack = 0,
                        HaveOffer = true,
                        ExtraData = "",
                        Badge = "",
                        OfferId = 0
                    });
                }
                deal.ItemDataList = itemDataList;
            }

            _deals.Add(deal.Id, deal);
        }

        var pages = await connection.QueryAsync<CatalogPage>("SELECT `id`,`parent_id`,`caption`,`page_link` as `link`,`visible`,`enabled`,`min_rank` as `minimumrank`,`min_vip` as `minimumvip`,`icon_image` as `icon`,`page_layout` as `layout`,`page_strings_1`,`page_strings_2` FROM `catalog_pages` ORDER BY `order_num`");
        foreach (CatalogPage page in pages)
        {
            if (_items.ContainsKey(page.Id))
            {
                page.Items = _items[page.Id];
                page.ItemOffers = page.Items.Values
                    .Where(item => item.HaveOffer && item.OfferId > 0)
                    .ToDictionary(item => item.OfferId, item => item);
            }

            page.PageStringsList1 = !string.IsNullOrWhiteSpace(page.PageStrings1) ? page.PageStrings1!.Split("|").ToList() : new();
            page.PageStringsList2 = !string.IsNullOrWhiteSpace(page.PageStrings2) ? page.PageStrings2!.Split("|").ToList() : new();
            _pages.Add(page.Id, page);
        }

        var bots = await connection.QueryAsync<CatalogBot>("SELECT `id`,`name`,`figure`,`motto`,`gender`,`ai_type` FROM `catalog_bot_presets`");
        foreach (CatalogBot bot in bots)
        {
            _botPresets.Add(bot.Id, bot);
        }

        var promotions = await connection.QueryAsync<CatalogPromotion>("SELECT `id`,`title`,`image`,`unknown`,`page_link`,`parent_id` FROM `catalog_promotions`");
        foreach(CatalogPromotion promotion in promotions)
        {
            if (_promotions.ContainsKey(promotion.Id))
                continue;

            _promotions.Add(promotion.Id, promotion);
        }

        _petRaceManager.Init();
        _clothingManager.Init();
        _logger.LogInformation("Catalog Manager -> LOADED");
    }

    public bool TryGetBot(uint itemId, out CatalogBot? bot)
    {
        if (_botPresets.TryGetValue(itemId, out var preset))
        {
            bot = preset;
            return true;
        }
        bot = null;
        return false;
    }

    public bool TryGetPage(int pageId, out CatalogPage? page)
    {
        if (_pages.TryGetValue(pageId, out var catalogPage))
        {
            page = catalogPage;
            return true;
        }
        page = null;
        return false;
    }

    public bool TryGetDeal(int dealId, out CatalogDeal? deal)
    {
        if (_deals.TryGetValue(dealId, out var catalogDeal))
        {
            deal = catalogDeal;
            return true;
        }
        deal = null;
        return false;
    }

    public ICollection<CatalogPage> Pages => _pages.Values;

    public ICollection<CatalogPromotion> Promotions => _promotions.Values;

    private static bool ParseBooleanish(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Equals("1", StringComparison.OrdinalIgnoreCase)
               || value.Equals("true", StringComparison.OrdinalIgnoreCase)
               || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CatalogItemRow
    {
        public int Id { get; set; }
        public uint ItemId { get; set; }
        public int Amount { get; set; }
        public int CostCredits { get; set; }
        public string ExtraData { get; set; } = string.Empty;
        public string HaveOfferRaw { get; set; } = string.Empty;
        public string CatalogName { get; set; } = string.Empty;
        public int PageId { get; set; }
        public int CostPixels { get; set; }
        public uint LimitedEditionStack { get; set; }
        public uint LimitedEditionSells { get; set; }
        public int CostDiamonds { get; set; }
        public string Badge { get; set; } = string.Empty;
        public int OfferId { get; set; }
    }
}
