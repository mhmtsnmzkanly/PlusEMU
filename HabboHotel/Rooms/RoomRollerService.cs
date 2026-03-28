using System.Drawing;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public class RoomRollerService : IRoomRollerService
{
    public List<Item> GetItemsOnRoller(Room room, Item roller)
    {
        var itemsOnRoller = room.GetGameMap().GetRoomItemForSquare(roller.GetX, roller.GetY, roller.GetZ);
        if (itemsOnRoller.Count > 10)
            return itemsOnRoller.Take(10).ToList();

        return itemsOnRoller;
    }

    public RoomRollerTargetState GetTargetState(List<Item> itemsOnNext)
    {
        var nextRollerHeight = 0.0;
        var nextSquareIsRoller = false;

        foreach (var item in itemsOnNext)
        {
            if (!item.IsRoller)
                continue;

            if (item.TotalHeight > nextRollerHeight)
                nextRollerHeight = item.TotalHeight;

            nextSquareIsRoller = true;
        }

        var nextRollerClear = true;
        if (nextSquareIsRoller)
        {
            foreach (var item in itemsOnNext)
            {
                if (item.TotalHeight > nextRollerHeight)
                    nextRollerClear = false;
            }
        }

        return new()
        {
            NextSquareIsRoller = nextSquareIsRoller,
            NextRollerClear = nextRollerClear,
            NextRollerHeight = nextRollerHeight
        };
    }

    public bool CanMoveItem(Room room, Item roller, Item? rollerItem, Point nextSquare, RoomRollerTargetState targetState, ICollection<uint> movedItemIds)
    {
        if (rollerItem == null)
            return false;

        return !movedItemIds.Contains(rollerItem.Id) &&
               room.GetGameMap().CanRollItemHere(nextSquare.X, nextSquare.Y) &&
               targetState.NextRollerClear &&
               roller.GetZ < rollerItem.GetZ &&
               room.GetRoomUserManager().GetUserForSquare(nextSquare.X, nextSquare.Y) == null;
    }

    public bool CanMoveUser(Room room, Item roller, RoomUser? rollerUser, Point nextSquare, RoomRollerTargetState targetState, ICollection<int> movedUserIds)
    {
        if (rollerUser == null || rollerUser.IsWalking || movedUserIds.Contains(rollerUser.HabboId))
            return false;

        return targetState.NextRollerClear &&
               room.GetGameMap().IsValidStep(new(roller.GetX, roller.GetY), new(nextSquare.X, nextSquare.Y), true, false, true) &&
               room.GetGameMap().CanRollItemHere(nextSquare.X, nextSquare.Y) &&
               room.GetGameMap().GetFloorStatus(nextSquare) != 0;
    }
}
