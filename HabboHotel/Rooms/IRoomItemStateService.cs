using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public interface IRoomItemStateService
{
    RoomItemStateInitializationResult InitializeLoadedFloorItem(Room room, Item item);
    void InitializeWallItemState(Room room, Item item);
    void EnsureTonerData(Room room, Item item);
}
