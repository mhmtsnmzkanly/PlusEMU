using System.Collections.Concurrent;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public class RoomItemUpdateQueueService : IRoomItemUpdateQueueService
{
    public void Process(ConcurrentQueue<Item> queue)
    {
        if (queue.Count == 0)
            return;

        var pendingItems = DequeueItemsNeedingFurtherUpdates(queue);
        RequeuePendingItems(queue, pendingItems);
    }

    private static List<Item> DequeueItemsNeedingFurtherUpdates(ConcurrentQueue<Item> queue)
    {
        var pendingItems = new List<Item>();
        while (queue.Count > 0)
        {
            if (!queue.TryDequeue(out var item) || item == null)
                continue;

            item.ProcessUpdates();
            if (item.UpdateCounter > 0)
                pendingItems.Add(item);
        }

        return pendingItems;
    }

    private static void RequeuePendingItems(ConcurrentQueue<Item> queue, List<Item> pendingItems)
    {
        foreach (var item in pendingItems)
        {
            if (item == null)
                continue;

            queue.Enqueue(item);
        }
    }
}
