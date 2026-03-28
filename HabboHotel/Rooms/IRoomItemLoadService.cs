using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public interface IRoomItemLoadService
{
    void ResetLoadedFurnitureState(ICollection<Item> floorItems, ICollection<Item> wallItems);
    void EnsureOwnedItemUser(Room room, Item item);
    bool TryRecoverInvalidFloorItem(Room room, Item item);
    void NormalizeWallItemPosition(Room room, Item item, string defaultWallPosition, Func<string, string?> wallPositionCheck);
}
