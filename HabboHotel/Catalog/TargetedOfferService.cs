using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;
using Plus.Utilities;

namespace Plus.HabboHotel.Catalog;

public sealed class TargetedOfferService : ITargetedOfferService
{
    private readonly ITargetedOfferManager _targetedOfferManager;
    private readonly ICatalogManager _catalogManager;
    private readonly ICatalogService _catalogService;
    private readonly IDatabase _database;
    private readonly ILogger<TargetedOfferService> _logger;

    public TargetedOfferService(
        ITargetedOfferManager targetedOfferManager,
        ICatalogManager catalogManager,
        ICatalogService catalogService,
        IDatabase database,
        ILogger<TargetedOfferService> logger)
    {
        _targetedOfferManager = targetedOfferManager;
        _catalogManager = catalogManager;
        _catalogService = catalogService;
        _database = database;
        _logger = logger;
    }

    public async Task SendCurrentOffer(GameClient session)
    {
        if (session.GetHabbo() is not { } habbo)
            return;

        if (!_targetedOfferManager.TryGetActiveOffer(out var offer) || offer == null)
            return;

        var purchaseState = await GetOrCreatePurchaseState(habbo, offer.Id);
        session.Send(new TargetedOfferComposer(offer, purchaseState));
    }

    public async Task Purchase(GameClient session, int offerId, int amount)
    {
        if (amount <= 0 || session.GetHabbo() is not { } habbo)
            return;

        if (!_targetedOfferManager.TryGetOffer(offerId, out var offer) || offer == null)
            return;

        if (!TryResolveCatalogItem(offer.CatalogItemId, out var item) || item == null)
        {
            _logger.LogWarning("Targeted offer {OfferId} points to missing catalog item {CatalogItemId}.", offerId, offer.CatalogItemId);
            return;
        }

        var purchaseState = await GetOrCreatePurchaseState(habbo, offerId);
        var remainingAmount = Math.Max(offer.PurchaseLimit - purchaseState.Amount, 0);
        if (remainingAmount <= 0)
            return;

        var purchaseAmount = Math.Min(remainingAmount, amount);
        if (item.IsLimited)
            purchaseAmount = 1;

        var totalCredits = item.CostCredits * purchaseAmount;
        var totalDuckets = item.CostPixels * purchaseAmount;
        var totalDiamonds = item.CostDiamonds * purchaseAmount;
        if (habbo.Credits < totalCredits || habbo.Duckets < totalDuckets || habbo.Diamonds < totalDiamonds)
            return;

        await _catalogService.PurchaseItem(session, item.PageId, item.Id, string.Empty, purchaseAmount);

        purchaseState.Amount += purchaseAmount;
        purchaseState.LastPurchaseTimestamp = (int)UnixTimestamp.GetNow();
        await PersistPurchaseState(habbo.Id, purchaseState);
    }

    public async Task SetState(GameClient session, int offerId, int state)
    {
        if (session.GetHabbo() is not { } habbo)
            return;

        var purchaseState = await GetOrCreatePurchaseState(habbo, offerId);
        purchaseState.State = state;
        await PersistPurchaseState(habbo.Id, purchaseState);
    }

    public async Task MarkViewed(GameClient session, int? offerId = null)
    {
        if (session.GetHabbo() is not { } habbo)
            return;

        var resolvedOfferId = offerId.GetValueOrDefault();
        if (resolvedOfferId <= 0)
        {
            if (!_targetedOfferManager.TryGetActiveOffer(out var activeOffer) || activeOffer == null)
                return;
            resolvedOfferId = activeOffer.Id;
        }

        await SetState(session, resolvedOfferId, 1);
    }

    private bool TryResolveCatalogItem(int catalogItemId, out CatalogItem? item)
    {
        item = null;
        foreach (var page in _catalogManager.Pages)
        {
            if (page.Items.TryGetValue(catalogItemId, out item))
                return true;
        }

        return false;
    }

    private async Task<TargetedOfferPurchaseState> GetOrCreatePurchaseState(Habbo habbo, int offerId)
    {
        if (habbo.TargetedOfferPurchases.TryGetValue(offerId, out var purchaseState))
            return purchaseState;

        purchaseState = await LoadPurchaseState(habbo.Id, offerId)
            ?? new TargetedOfferPurchaseState { OfferId = offerId };

        habbo.TargetedOfferPurchases[offerId] = purchaseState;
        await EnsurePurchaseStateExists(habbo.Id, offerId);
        return purchaseState;
    }

    private async Task<TargetedOfferPurchaseState?> LoadPurchaseState(int userId, int offerId)
    {
        try
        {
            using var connection = _database.Connection();
            return await connection.QueryFirstOrDefaultAsync<TargetedOfferPurchaseState>(
                """
                SELECT
                    `offer_id` AS OfferId,
                    `state` AS State,
                    `amount` AS Amount,
                    `last_purchase` AS LastPurchaseTimestamp
                FROM `users_target_offer_purchases`
                WHERE `user_id` = @userId AND `offer_id` = @offerId
                LIMIT 1;
                """,
                new { userId, offerId });
        }
        catch (MySqlException e) when (e.Message.Contains("users_target_offer_purchases"))
        {
            _logger.LogWarning("Skipping targeted offer purchase load because table users_target_offer_purchases is missing.");
            return null;
        }
    }

    private async Task EnsurePurchaseStateExists(int userId, int offerId)
    {
        try
        {
            using var connection = _database.Connection();
            await connection.ExecuteAsync(
                """
                INSERT INTO `users_target_offer_purchases` (`user_id`, `offer_id`)
                VALUES (@userId, @offerId)
                ON DUPLICATE KEY UPDATE `offer_id` = VALUES(`offer_id`);
                """,
                new { userId, offerId });
        }
        catch (MySqlException e) when (e.Message.Contains("users_target_offer_purchases"))
        {
            _logger.LogWarning("Skipping targeted offer purchase create because table users_target_offer_purchases is missing.");
        }
    }

    private async Task PersistPurchaseState(int userId, TargetedOfferPurchaseState purchaseState)
    {
        try
        {
            using var connection = _database.Connection();
            await connection.ExecuteAsync(
                """
                INSERT INTO `users_target_offer_purchases` (`user_id`, `offer_id`, `state`, `amount`, `last_purchase`)
                VALUES (@userId, @offerId, @state, @amount, @lastPurchaseTimestamp)
                ON DUPLICATE KEY UPDATE
                    `state` = VALUES(`state`),
                    `amount` = VALUES(`amount`),
                    `last_purchase` = VALUES(`last_purchase`);
                """,
                new
                {
                    userId,
                    purchaseState.OfferId,
                    purchaseState.State,
                    purchaseState.Amount,
                    purchaseState.LastPurchaseTimestamp
                });
        }
        catch (MySqlException e) when (e.Message.Contains("users_target_offer_purchases"))
        {
            _logger.LogWarning("Skipping targeted offer purchase persist because table users_target_offer_purchases is missing.");
        }
    }
}
