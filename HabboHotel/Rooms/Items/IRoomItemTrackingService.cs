using Plus.Communication.Packets;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public interface IRoomItemTrackingService
{
    void RegisterLoadedItem(IDictionary<uint, Item> floorItems, IDictionary<uint, Item> wallItems, Item item);
    bool TryGetLoadedItem(IReadOnlyDictionary<uint, Item> floorItems, IReadOnlyDictionary<uint, Item> wallItems, uint itemId, out Item? item);
    void RemoveLoadedItem(Room room, IDictionary<uint, Item> floorItems, IDictionary<uint, Item> wallItems, Item item);
    void TrackMovedItem(IDictionary<uint, Item> movedItems, Item item);
    void RemoveTrackedItem(IDictionary<uint, Item> movedItems, IDictionary<uint, Item> rollers, Item item);
    void DestroyLoadedItems(IEnumerable<Item> items);
    void ClearTrackedState(IDictionary<uint, Item> movedItems, IDictionary<uint, Item> rollers, IDictionary<uint, Item> wallItems, IDictionary<uint, Item> floorItems, ICollection<uint> rollerItemsMoved, ICollection<int> rollerUsersMoved, ICollection<IServerPacket> rollerMessages);
}
