using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public interface IRoomItemPersistenceService
{
    void SaveMovedItems(IEnumerable<Item> items);
}
