using System.Globalization;
using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Inventory.AvatarEffects;
using Plus.Communication.Packets.Outgoing.Inventory.Bots;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Inventory.Pets;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Core;
using Plus.Core.Settings;
using Plus.Database;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.Catalog.Utilities;
using Plus.HabboHotel.Catalog.Vouchers;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users.Effects;

namespace Plus.HabboHotel.Catalog;

internal class CatalogService : ICatalogService
{
    private readonly ICatalogManager _catalogManager;
    private readonly IVoucherManager _voucherManager;
    private readonly IDatabase _database;
    private readonly ISettingsManager _settingsManager;
    private readonly IAchievementService _achievementService;
    private readonly IItemDataManager _itemManager;
    private readonly IBadgeManager _badgeManager;
    private readonly IItemFactory _itemFactory;
    private readonly IBotUtility _botUtility;
    private readonly IPetUtility _petUtility;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(ICatalogManager catalogManager,
        IVoucherManager voucherManager,
        IDatabase database,
        ISettingsManager settingsManager,
        IAchievementService achievementService,
        IItemDataManager itemManager,
        IBadgeManager badgeManager,
        IItemFactory itemFactory,
        IBotUtility botUtility,
        IPetUtility petUtility,
        ILogger<CatalogService> logger)
    {
        _catalogManager = catalogManager;
        _voucherManager = voucherManager;
        _database = database;
        _settingsManager = settingsManager;
        _achievementService = achievementService;
        _itemManager = itemManager;
        _badgeManager = badgeManager;
        _itemFactory = itemFactory;
        _botUtility = botUtility;
        _petUtility = petUtility;
        _logger = logger;
    }

    public async Task RedeemVoucher(GameClient session, string code)
    {
        var habbo = session.GetHabbo();
        if (habbo == null) return;

        code = code.Replace("\r", "");
        if (!_voucherManager.TryGetVoucher(code, out var voucher))
        {
            session.Send(new VoucherRedeemErrorComposer(0));
            return;
        }

        if (voucher!.CurrentUses >= voucher.MaxUses)
        {
            session.SendNotification("This voucher has reached its maximum usage limit.");
            return;
        }

        using var connection = _database.Connection();
        var exists = connection.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM `user_vouchers` WHERE `user_id` = @userId AND `voucher_id` = @voucherCode LIMIT 1",
            new { userId = habbo.Id, voucherCode = code });

        if (exists > 0)
        {
            session.SendNotification("You have already used this voucher code!");
            return;
        }

        connection.Execute("INSERT INTO `user_vouchers` (`user_id`, `voucher_id`) VALUES (@userId, @voucherCode)",
            new { userId = habbo.Id, voucherCode = code });

        _voucherManager.UpdateUses(voucher);

        if (voucher.Type == VoucherType.Credit)
        {
            habbo.Credits += voucher.Value;
            session.Send(new CreditBalanceComposer(habbo.Credits));
        }
        else if (voucher.Type == VoucherType.Ducket)
        {
            habbo.Duckets += voucher.Value;
            session.Send(new HabboActivityPointNotificationComposer(habbo.Duckets, voucher.Value));
        }

        session.Send(new VoucherRedeemOkComposer());
    }

    public async Task PurchaseItem(GameClient session, int pageId, int itemId, string extraData, int amount)
    {
        var habbo = session.GetHabbo();
        var inventory = habbo?.Inventory;
        if (habbo?.Permissions == null || inventory == null || habbo.Effects == null)
        {
            _logger.LogWarning("PurchaseItem aborted for session {sessionId}: missing habbo or components.", session.Id);
            return;
        }

        if (_settingsManager.TryGetValue("catalog.enabled") != "1")
        {
            _logger.LogWarning("PurchaseItem aborted for session {sessionId}: catalog disabled.", session.Id);
            session.SendNotification("The hotel managers have disabled the catalogue");
            return;
        }

        if (!_catalogManager.TryGetPage(pageId, out var page))
        {
            _logger.LogWarning("PurchaseItem aborted for session {sessionId}: page {pageId} not found.", session.Id, pageId);
            return;
        }
        if (!page.Enabled || !page.Visible || page.MinimumRank > habbo.Rank || (page.MinimumVip > habbo.VipRank && habbo.Rank == 1))
        {
            _logger.LogWarning("PurchaseItem aborted for session {sessionId}: page {pageId} not accessible.", session.Id, pageId);
            return;
        }

        if (!page.Items.TryGetValue(itemId, out var item))
        {
            if (page.ItemOffers.ContainsKey(itemId))
            {
                item = page.ItemOffers[itemId];
                if (item == null)
                {
                    _logger.LogWarning("PurchaseItem aborted for session {sessionId}: page {pageId} offer {itemId} resolved null.", session.Id, pageId, itemId);
                    return;
                }
            }
            else
            {
                _logger.LogWarning("PurchaseItem aborted for session {sessionId}: page {pageId} item/offer {itemId} not found.", session.Id, pageId, itemId);
                return;
            }
        }

        if (amount < 1 || amount > 100 || !item.HaveOffer) amount = 1;

        var amountPurchase = item.Amount > 1 ? item.Amount : amount;
        var totalCreditsCost = amount > 1 ? item.CostCredits * amount - (int)Math.Floor((double)amount / 6) * item.CostCredits : item.CostCredits;
        var totalPixelCost = amount > 1 ? item.CostPixels * amount - (int)Math.Floor((double)amount / 6) * item.CostPixels : item.CostPixels;
        var totalDiamondCost = amount > 1 ? item.CostDiamonds * amount - (int)Math.Floor((double)amount / 6) * item.CostDiamonds : item.CostDiamonds;

        if (habbo.Credits < totalCreditsCost || habbo.Duckets < totalPixelCost || habbo.Diamonds < totalDiamondCost)
        {
            _logger.LogWarning("PurchaseItem aborted for session {sessionId}: insufficient balance. Credits {credits}/{neededCredits}, Duckets {duckets}/{neededDuckets}, Diamonds {diamonds}/{neededDiamonds}.",
                session.Id, habbo.Credits, totalCreditsCost, habbo.Duckets, totalPixelCost, habbo.Diamonds, totalDiamondCost);
            return;
        }

        // Interaction Type validation and extraData normalization
        switch (item.Definition!.InteractionType)
        {
            case InteractionType.Pet:
                var bits = extraData.Split('\n');
                if (bits.Length < 3 || !_petUtility.CheckPetName(bits[0]) || bits[1].Length > 2 || bits[2].Length != 6) return;
                await _achievementService.ProgressAchievement(session, "ACH_PetLover", 1);
                break;
            case var _ when item.Definition.IsRoomDecoration:
                double.TryParse(extraData, NumberStyles.Any, CultureInfo.InvariantCulture, out var number);
                extraData = number.ToString(CultureInfo.InvariantCulture);
                break;
            case InteractionType.Postit: extraData = "FFFF33"; break;
            case var _ when item.Definition.IsMoodlight: extraData = "1,1,1,#000000,255"; break;
            case InteractionType.Trophy: extraData = $"{habbo.Username}{Convert.ToChar(9)}{DateTime.Now:dd-MM-yyyy}{Convert.ToChar(9)}{extraData}"; break;
            case InteractionType.Mannequin: extraData = $"m{Convert.ToChar(5)}.ch-210-1321.lg-285-92{Convert.ToChar(5)}Default Mannequin"; break;
            case InteractionType.BadgeDisplay:
                if (!inventory.Badges.HasBadge(extraData))
                {
                    session.Send(new BroadcastMessageAlertComposer("Oops, it appears that you do not own this badge."));
                    return;
                }
                extraData = $"{extraData}{Convert.ToChar(9)}{habbo.Username}{Convert.ToChar(9)}{DateTime.Now:dd-MM-yyyy}";
                break;
            case InteractionType.Badge:
                if (inventory.Badges.HasBadge(item.Definition.ItemName))
                {
                    session.Send(new PurchaseErrorComposer(1));
                    return;
                }
                break;
        }

        uint limitedEditionSells = 0, limitedEditionStack = 0;
        if (item.IsLimited)
        {
            if (item.LimitedEditionStack <= item.LimitedEditionSells)
            {
                session.SendNotification("This item has sold out!");
                session.Send(new CatalogUpdatedComposer());
                session.Send(new PurchaseOkComposer());
                return;
            }
            item.LimitedEditionSells++;
            using var connection = _database.Connection();
            connection.Execute("UPDATE `catalog_items` SET `limited_sells` = @limitedSells WHERE `id` = @itemId LIMIT 1",
                new { limitedSells = item.LimitedEditionSells, itemId = item.Id });
            limitedEditionSells = item.LimitedEditionSells;
            limitedEditionStack = item.LimitedEditionStack;
        }

        // Deduct currencies
        if (totalCreditsCost > 0) { habbo.Credits -= totalCreditsCost; session.Send(new CreditBalanceComposer(habbo.Credits)); }
        if (totalPixelCost > 0) { habbo.Duckets -= totalPixelCost; session.Send(new HabboActivityPointNotificationComposer(habbo.Duckets, habbo.Duckets)); }
        if (totalDiamondCost > 0) { habbo.Diamonds -= totalDiamondCost; session.Send(new HabboActivityPointNotificationComposer(habbo.Diamonds, 0, 5)); }

        // Deliver item
        var itemType = item.Definition.Type.ToString().ToLower();
        switch (itemType)
        {
            case "s": // Floor
            case "i": // Wall
                await DeliverFurniture(session, item, habbo, extraData, amountPurchase, limitedEditionSells, limitedEditionStack);
                break;
            case "e": // Effect
                var effect = habbo.Effects.HasEffect(item.Definition.SpriteId) ? habbo.Effects.GetEffectNullable(item.Definition.SpriteId) : habbo.Effects.CreateEffect(item.Definition.SpriteId, 3600);
                effect?.AddToQuantity(_database);
                session.Send(new AvatarEffectAddedComposer(item.Definition.SpriteId, 3600));
                break;
            case "r": // Bot
                var bot = _botUtility.CreateBot(item.Definition, habbo.Id);
                if (bot != null) { inventory.Bots.AddBot(bot); session.Send(new BotInventoryComposer(inventory.Bots.Bots.Values.ToList())); session.Send(new FurniListNotificationComposer((uint)bot.Id, 5)); }
                break;
            case "b": // Badge
                await _badgeManager.GiveBadge(habbo, item.Definition.ItemName);
                session.Send(new FurniListNotificationComposer(0, 4));
                break;
            case "p": // Pet
                var petData = extraData.Split('\n');
                var pet = _petUtility.CreatePet(habbo.Id, petData[0], item.Definition.BehaviourData, petData[1], petData[2]);
                if (pet != null && inventory.Pets.AddPet(pet))
                {
                    session.Send(new FurniListNotificationComposer((uint)pet.PetId, 3));
                    session.Send(new PetInventoryComposer(inventory.Pets.Pets.Values.ToList()));
                }
                break;
        }

        if (!string.IsNullOrEmpty(item.Badge) && _badgeManager.Badges.TryGetValue(item.Badge, out var badge) && (string.IsNullOrEmpty(badge.RequiredRight) || habbo.Permissions.HasRight(badge.RequiredRight)))
            await _badgeManager.GiveBadge(habbo, badge.Code);

        session.Send(new PurchaseOkComposer(item, item.Definition));
        session.Send(new FurniListUpdateComposer());
    }

    private async Task DeliverFurniture(GameClient session, CatalogItem item, Plus.HabboHotel.Users.Habbo habbo, string extraData, int amount, uint selectionSells, uint selectionStack)
    {
        var generatedItems = new List<Item>();
        switch (item.Definition!.InteractionType)
        {
            case InteractionType.Arrow:
            case InteractionType.Teleport:
                for (var i = 0; i < amount; i++) generatedItems.AddRange(_itemFactory.CreateTeleporterItems(item.Definition, habbo)!);
                break;
            case var _ when item.Definition.IsMoodlight:
                var moodItems = amount > 1 ? _itemFactory.CreateMultipleItems(item.Definition, habbo, extraData, amount) : new List<Item> { _itemFactory.CreateSingleItemNullable(item.Definition, habbo, extraData, extraData)! };
                foreach (var i in moodItems!) { generatedItems.Add(i); _itemFactory.CreateMoodlightData(i); }
                break;
            case var _ when item.Definition.IsToner:
                var tonerItems = amount > 1 ? _itemFactory.CreateMultipleItems(item.Definition, habbo, extraData, amount) : new List<Item> { _itemFactory.CreateSingleItemNullable(item.Definition, habbo, extraData, extraData)! };
                foreach (var i in tonerItems!) { generatedItems.Add(i); _itemFactory.CreateTonerData(i); }
                break;
            case var _ when item.Definition.IsDeal:
                if (_catalogManager.TryGetDeal(item.Definition.BehaviourData, out var deal))
                    foreach (var dealItem in deal.ItemDataList) generatedItems.AddRange(_itemFactory.CreateMultipleItems(dealItem.Definition, habbo, "", amount)!);
                break;
            default:
                if (amount > 1) generatedItems.AddRange(_itemFactory.CreateMultipleItems(item.Definition, habbo, extraData, amount)!);
                else generatedItems.Add(_itemFactory.CreateSingleItemNullable(item.Definition, habbo, extraData, extraData, 0, selectionSells, selectionStack)!);
                break;
        }

        _logger.LogInformation("PurchaseItem delivery prepared for session {sessionId}. PageId: {pageId}. ItemId: {itemId}. GeneratedCount: {generatedCount}. Type: {itemType}.",
            session.Id, item.PageId, item.Id, generatedItems.Count, item.Definition.Type);

        foreach (var purchasedItem in generatedItems)
            if (habbo.Inventory!.Furniture.AddItem(purchasedItem.ToInventoryItem()))
                session.Send(new FurniListNotificationComposer(purchasedItem.Id, 1));
    }
}
