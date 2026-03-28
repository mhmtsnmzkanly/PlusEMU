using System.Drawing;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired;

internal static class WiredFloorMoveHelper
{
    public static bool TryMoveFloorItem(Room room, Item item, Point targetPoint, out double newZ, Func<double>? getInitialZ = null)
    {
        newZ = getInitialZ?.Invoke() ?? item.GetZ;

        var gameMap = room.GetGameMap();
        if (!gameMap.ItemCanMove(item, targetPoint))
            return false;

        if (!gameMap.CanRollItemHere(targetPoint.X, targetPoint.Y) || gameMap.SquareHasUsers(targetPoint.X, targetPoint.Y))
            return false;

        var canBePlaced = true;
        foreach (var coordinatedItem in gameMap.GetCoordinatedItems(targetPoint).ToList())
        {
            if (coordinatedItem == null || coordinatedItem.Id == item.Id)
                continue;

            if (!coordinatedItem.Definition.Walkable)
            {
                canBePlaced = false;
                break;
            }

            if (coordinatedItem.TotalHeight > newZ)
                newZ = coordinatedItem.TotalHeight;

            if (canBePlaced && !coordinatedItem.Definition.Stackable)
                canBePlaced = false;
        }

        if (!canBePlaced || targetPoint == item.Coordinate)
            return false;

        room.SendPacket(new SlideObjectBundleComposer(item.GetX, item.GetY, item.GetZ, targetPoint.X, targetPoint.Y, newZ, 0, 0, item.Id));
        room.GetRoomItemHandler().SetFloorItem(item, targetPoint.X, targetPoint.Y, newZ);
        return true;
    }
}
