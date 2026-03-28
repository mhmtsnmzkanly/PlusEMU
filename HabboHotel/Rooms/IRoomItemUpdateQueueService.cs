using System.Collections.Concurrent;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public interface IRoomItemUpdateQueueService
{
    void Process(ConcurrentQueue<Item> queue);
}
