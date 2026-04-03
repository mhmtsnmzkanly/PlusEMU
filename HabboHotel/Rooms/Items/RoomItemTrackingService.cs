using Plus.Communication.Packets;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public class RoomItemTrackingService : IRoomItemTrackingService
{
    public void RegisterLoadedItem(IDictionary<uint, Item> floorItems, IDictionary<uint, Item> wallItems, Item item)
    {
        if (item.IsFloorItem)
        {
            if (!floorItems.ContainsKey(item.Id))
                floorItems[item.Id] = item;
            return;
        }

        if (item.IsWallItem && !wallItems.ContainsKey(item.Id))
            wallItems[item.Id] = item;
    }

    public bool TryGetLoadedItem(IReadOnlyDictionary<uint, Item> floorItems, IReadOnlyDictionary<uint, Item> wallItems, uint itemId, out Item? item)
    {
        if (floorItems.TryGetValue(itemId, out var floorItem))
        {
            item = floorItem;
            return true;
        }

        if (wallItems.TryGetValue(itemId, out var wallItem))
        {
            item = wallItem;
            return true;
        }

        item = null;
        return false;
    }

    public void RemoveLoadedItem(Room room, IDictionary<uint, Item> floorItems, IDictionary<uint, Item> wallItems, Item item)
    {
        if (item.IsWallItem)
        {
            wallItems.Remove(item.Id);
            return;
        }

        if (!floorItems.Remove(item.Id, out var removedItem))
            return;

        room.GetGameMap().RemoveFromMap(removedItem);
    }

    public void TrackMovedItem(IDictionary<uint, Item> movedItems, Item item)
    {
        if (!movedItems.ContainsKey(item.Id))
            movedItems[item.Id] = item;
    }

    public void RemoveTrackedItem(IDictionary<uint, Item> movedItems, IDictionary<uint, Item> rollers, Item item)
    {
        movedItems.Remove(item.Id);
        rollers.Remove(item.Id);
    }

    public void DestroyLoadedItems(IEnumerable<Item> items)
    {
        foreach (var item in items.ToList())
        {
            if (item == null)
                continue;

            item.Destroy();
        }
    }

    public void ClearTrackedState(IDictionary<uint, Item> movedItems, IDictionary<uint, Item> rollers, IDictionary<uint, Item> wallItems, IDictionary<uint, Item> floorItems, ICollection<uint> rollerItemsMoved, ICollection<int> rollerUsersMoved, ICollection<IServerPacket> rollerMessages)
    {
        movedItems.Clear();
        rollers.Clear();
        wallItems.Clear();
        floorItems.Clear();
        rollerItemsMoved.Clear();
        rollerUsersMoved.Clear();
        rollerMessages.Clear();
    }
}
