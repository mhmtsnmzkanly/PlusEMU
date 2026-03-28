using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public interface IRoomItemRemovalService
{
    void PrepareItemRemoval(Room room, GameClient? session, Item item);
    void BroadcastItemRemoval(Room room, Item item);
}
