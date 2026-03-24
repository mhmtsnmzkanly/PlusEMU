using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Items;

public class ItemDataManager : IItemDataManager
{
    private sealed class FurnitureRow
    {
        public uint Id { get; init; }
        public int SpriteId { get; init; }
        public string ItemName { get; init; } = string.Empty;
        public string PublicName { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public int Width { get; init; }
        public int Length { get; init; }
        public double StackHeight { get; init; }
        public string CanStack { get; init; } = "0";
        public string IsWalkable { get; init; } = "0";
        public string CanSit { get; init; } = "0";
        public string AllowRecycle { get; init; } = "0";
        public string AllowTrade { get; init; } = "0";
        public string AllowMarketplaceSell { get; init; } = "0";
        public string AllowGift { get; init; } = "0";
        public string AllowInventoryStack { get; init; } = "0";
        public string InteractionType { get; init; } = string.Empty;
        public int BehaviourData { get; init; }
        public int InteractionModesCount { get; init; }
        public string VendingIds { get; init; } = string.Empty;
        public string HeightAdjustable { get; init; } = string.Empty;
        public int EffectId { get; init; }
        public string IsRare { get; init; } = "0";
        public string ExtraRot { get; init; } = "0";
    }

    private readonly ILogger<ItemDataManager> _logger;
    private readonly IDatabase _database;
    public Dictionary<int, uint> Gifts { get; } = new(0); //<SpriteId, Item>
    public Dictionary<uint, ItemDefinition> Items { get; } = new(0);

    public ItemDataManager(ILogger<ItemDataManager> logger, IDatabase database)
    {
        _logger = logger;
        _database = database;
    }

    public void Init()
    {
        if (Items.Count > 0)
            Items.Clear();
        if (Gifts.Count > 0)
            Gifts.Clear();

        using (var connection = _database.Connection())
        {
            foreach (var row in connection.Query<FurnitureRow>(
                         """
                         SELECT
                             `id` AS Id,
                             `sprite_id` AS SpriteId,
                             `item_name` AS ItemName,
                             `public_name` AS PublicName,
                             `type` AS Type,
                             `width` AS Width,
                             `length` AS Length,
                             `stack_height` AS StackHeight,
                             `can_stack` AS CanStack,
                             `is_walkable` AS IsWalkable,
                             `can_sit` AS CanSit,
                             `allow_recycle` AS AllowRecycle,
                             `allow_trade` AS AllowTrade,
                             `allow_marketplace_sell` AS AllowMarketplaceSell,
                             `allow_gift` AS AllowGift,
                             `allow_inventory_stack` AS AllowInventoryStack,
                             `interaction_type` AS InteractionType,
                             `behaviour_data` AS BehaviourData,
                             `interaction_modes_count` AS InteractionModesCount,
                             `vending_ids` AS VendingIds,
                             `height_adjustable` AS HeightAdjustable,
                             `effect_id` AS EffectId,
                             `is_rare` AS IsRare,
                             `extra_rot` AS ExtraRot
                         FROM `furniture`
                         """))
            {
                try
                {
                    var definition = new ItemDefinition
                    {
                        Id = row.Id,
                        SpriteId = row.SpriteId,
                        ItemName = row.ItemName,
                        PublicName = row.PublicName,
                        Type = string.Equals(row.Type, "s", StringComparison.OrdinalIgnoreCase) ? ItemType.Floor : ItemType.Wall,
                        Width = row.Width,
                        Length = row.Length,
                        Height = row.StackHeight,
                        Stackable = row.CanStack == "1",
                        Walkable = row.IsWalkable == "1",
                        IsSeat = row.CanSit == "1",
                        AllowEcotronRecycle = row.AllowRecycle == "1",
                        AllowTrade = row.AllowTrade == "1",
                        AllowMarketplaceSell = row.AllowMarketplaceSell == "1",
                        AllowGift = row.AllowGift == "1",
                        AllowInventoryStack = row.AllowInventoryStack == "1",
                        InteractionType = InteractionTypes.GetTypeFromString(row.InteractionType),
                        BehaviourData = row.BehaviourData,
                        Modes = row.InteractionModesCount,
                        VendingIds = (!string.IsNullOrEmpty(row.VendingIds) && row.VendingIds != "0")
                                ? row.VendingIds.Split(",").Select(int.Parse).ToList()
                                : new(0),
                        AdjustableHeights = (!string.IsNullOrEmpty(row.HeightAdjustable) && row.HeightAdjustable != "0")
                                ? row.HeightAdjustable.Split(",").Select(double.Parse).ToList()
                                : new(0),
                        EffectId = row.EffectId,
                        IsRare = row.IsRare == "1",
                        ExtraRot = row.ExtraRot == "1",
                    };

                    Gifts.TryAdd(definition.SpriteId, definition.Id);
                    Items.Add(definition.Id, definition);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.ReadKey();
                }
            }
        }
        _logger.LogInformation("Item Manager -> LOADED");
    }

    public ItemDefinition GetItemByName(string name)
    {
        foreach (var entry in Items)
        {
            var item = entry.Value;
            if (item.ItemName == name)
                return item;
        }
        return null!;
    }
}
