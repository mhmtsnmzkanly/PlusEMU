using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired;

internal static class WiredSetItemSelector
{
    public static bool TryGetRandomFloorItem(Room room, ConcurrentDictionary<uint, Item> setItems, [NotNullWhen(true)] out Item? item)
    {
        item = null;

        var items = setItems.Values.OrderBy(_ => Random.Shared.Next()).ToList();
        if (items.Count == 0)
            return false;

        item = items.FirstOrDefault();
        if (item == null)
            return false;

        if (room.GetRoomItemHandler().GetFloor.Contains(item))
            return true;

        setItems.TryRemove(item.Id, out item);
        if (item != null && items.Contains(item))
            items.Remove(item);
        if (setItems.Count == 0 || items.Count == 0)
            return false;

        item = items.FirstOrDefault();
        return item != null;
    }
}
