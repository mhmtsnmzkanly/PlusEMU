using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Plus.Core;
using Plus.Core.Settings;
using Plus.Database;

namespace Plus.HabboHotel.Catalog;

public sealed class TargetedOfferManager : ITargetedOfferManager, IStartable
{
    private readonly IDatabase _database;
    private readonly ISettingsManager _settingsManager;
    private readonly ILogger<TargetedOfferManager> _logger;
    private readonly Dictionary<int, TargetedOffer> _offers = new();
    private int _activeOfferId;

    public TargetedOfferManager(IDatabase database, ISettingsManager settingsManager, ILogger<TargetedOfferManager> logger)
    {
        _database = database;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public async Task Start()
    {
        _offers.Clear();

        try
        {
            using var connection = _database.Connection();
            var rows = await connection.QueryAsync<TargetedOfferRow>(
                """
                SELECT
                    `id` AS Id,
                    `catalog_item_id` AS CatalogItemId,
                    `offer_code` AS Identifier,
                    `credits` AS PriceInCredits,
                    `points` AS PriceInActivityPoints,
                    `points_type` AS ActivityPointsType,
                    `purchase_limit` AS PurchaseLimit,
                    `end_timestamp` AS EndTimestamp,
                    `title` AS Title,
                    `description` AS Description,
                    `image` AS ImageUrl,
                    `icon` AS Icon,
                    `variables` AS Variables
                FROM `catalog_target_offers`
                WHERE `enabled` = 1 AND `end_timestamp` > UNIX_TIMESTAMP();
                """);

            foreach (var row in rows)
            {
                _offers[row.Id] = new TargetedOffer
                {
                    Id = row.Id,
                    CatalogItemId = row.CatalogItemId,
                    Identifier = row.Identifier ?? string.Empty,
                    PriceInCredits = row.PriceInCredits,
                    PriceInActivityPoints = row.PriceInActivityPoints,
                    ActivityPointsType = row.ActivityPointsType,
                    PurchaseLimit = row.PurchaseLimit,
                    EndTimestamp = row.EndTimestamp,
                    Title = row.Title ?? string.Empty,
                    Description = row.Description ?? string.Empty,
                    ImageUrl = row.ImageUrl ?? string.Empty,
                    Icon = row.Icon ?? string.Empty,
                    Variables = string.IsNullOrWhiteSpace(row.Variables)
                        ? []
                        : row.Variables.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                };
            }
        }
        catch (MySqlException e) when (e.Message.Contains("catalog_target_offers"))
        {
            _logger.LogWarning("Skipping targeted offers load because table catalog_target_offers is missing.");
        }

        var configuredOfferId = _settingsManager.GetIntOrDefault("hotel.targetoffer.id", 0);
        if (configuredOfferId > 0 && _offers.ContainsKey(configuredOfferId))
            _activeOfferId = configuredOfferId;
        else
            _activeOfferId = _offers.Keys.OrderBy(id => id).FirstOrDefault();

        _logger.LogInformation("Loaded {OfferCount} targeted offers. ActiveOfferId: {ActiveOfferId}.", _offers.Count, _activeOfferId);
    }

    public bool TryGetActiveOffer(out TargetedOffer? offer) => _offers.TryGetValue(_activeOfferId, out offer);

    public bool TryGetOffer(int offerId, out TargetedOffer? offer) => _offers.TryGetValue(offerId, out offer);

    private sealed class TargetedOfferRow
    {
        public int Id { get; init; }
        public int CatalogItemId { get; init; }
        public string? Identifier { get; init; }
        public int PriceInCredits { get; init; }
        public int PriceInActivityPoints { get; init; }
        public int ActivityPointsType { get; init; }
        public int PurchaseLimit { get; init; }
        public int EndTimestamp { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? ImageUrl { get; init; }
        public string? Icon { get; init; }
        public string? Variables { get; init; }
    }
}
