using System.Drawing;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public interface IRoomRollerService
{
    List<Item> GetItemsOnRoller(Room room, Item roller);
    RoomRollerTargetState GetTargetState(List<Item> itemsOnNext);
    bool CanMoveItem(Room room, Item roller, Item? rollerItem, Point nextSquare, RoomRollerTargetState targetState, ICollection<uint> movedItemIds);
    bool CanMoveUser(Room room, Item roller, RoomUser? rollerUser, Point nextSquare, RoomRollerTargetState targetState, ICollection<int> movedUserIds);
}
