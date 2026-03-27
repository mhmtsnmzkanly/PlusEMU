using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Items;

public interface IItemLoader
{
    List<Item> GetItemsForRoom(uint roomId, Room room);
    List<InventoryItem> GetItemsForUser(uint userId);
    void DeleteAllInventoryItemsForUser(int userId);
}
