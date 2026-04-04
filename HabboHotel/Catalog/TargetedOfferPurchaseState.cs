namespace Plus.HabboHotel.Catalog;

public sealed class TargetedOfferPurchaseState
{
    public int OfferId { get; init; }
    public int State { get; set; }
    public int Amount { get; set; }
    public int LastPurchaseTimestamp { get; set; }
}
