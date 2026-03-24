using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Catalog;

public class CatalogItem
{
    public int Id { get; set; }
    public uint ItemId { get; set; }
    public ItemDefinition Definition { get; set; } = null!;
    public int Amount { get; set; }
    public int CostCredits { get; set; }
    public string ExtraData { get; set; } = string.Empty;
    public bool HaveOffer { get; set; }
    public bool IsLimited { get; set; }
    public string CatalogName { get; set; } = string.Empty;
    public int PageId { get; set; }
    public int CostPixels { get; set; }
    public uint LimitedEditionStack { get; set; }
    public uint LimitedEditionSells { get; set; }
    public int CostDiamonds { get; set; }
    public string Badge { get; set; } = string.Empty;
    public int OfferId { get; set; }
}
