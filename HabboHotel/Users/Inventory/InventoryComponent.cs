using Plus.HabboHotel.Users.Inventory.Badges;
using Plus.HabboHotel.Users.Inventory.Bots;
using Plus.HabboHotel.Users.Inventory.Furniture;
using Plus.HabboHotel.Users.Inventory.Pets;

namespace Plus.HabboHotel.Users.Inventory;

public class InventoryComponent
{
    public BadgesInventoryComponent Badges { get; init; } = null!;
    public FurnitureInventoryComponent Furniture { get; init; } = null!;
    public PetsInventoryComponent Pets { get; init; } = null!;
    public BotInventoryComponent Bots { get; init; } = null!;
}
