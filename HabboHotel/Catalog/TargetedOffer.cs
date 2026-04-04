namespace Plus.HabboHotel.Catalog;

public sealed class TargetedOffer
{
    public int Id { get; init; }
    public int CatalogItemId { get; init; }
    public string Identifier { get; init; } = string.Empty;
    public int PriceInCredits { get; init; }
    public int PriceInActivityPoints { get; init; }
    public int ActivityPointsType { get; init; }
    public int PurchaseLimit { get; init; }
    public int EndTimestamp { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string[] Variables { get; init; } = [];
}
