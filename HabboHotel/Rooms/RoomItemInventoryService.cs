using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Rooms;

public class RoomItemInventoryService : IRoomItemInventoryService
{
    public bool CanRemoveOwnedItem(Item? item, int ownerId) => item != null && item.UserId == ownerId;

    public void AddRemovedItemToInventory(Item? removedItem, FurnitureInventoryComponent inventory)
    {
        if (removedItem != null)
            inventory.AddItem(removedItem.ToInventoryItem());
    }
}
