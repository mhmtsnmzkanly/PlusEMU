using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Rooms;

public interface IRoomItemInventoryService
{
    bool CanRemoveOwnedItem(Item? item, int ownerId);
    void AddRemovedItemToInventory(Item? removedItem, FurnitureInventoryComponent inventory);
}
