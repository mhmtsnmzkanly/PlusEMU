using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms.PathFinding;

namespace Plus.HabboHotel.Rooms;

public class RoomItemPlacementValidatorService : IRoomItemPlacementValidatorService
{
    public bool CanPlaceNewFloorItem(Room room, Item item, bool newItem)
    {
        if (!newItem || !item.IsWired)
            return true;

        return !item.Definition.IsRegenerateMapsWired ||
               room.GetRoomItemHandler().GetFloor.Count(x => x.Definition.IsRegenerateMapsWired) == 0;
    }

    public bool HasConflictingRoller(Item item, List<Item> itemsOnTile) =>
        item.Definition.InteractionType == InteractionType.Roller &&
        itemsOnTile.Count(x => x.Definition.InteractionType == InteractionType.Roller && x.Id != item.Id) > 0;

    public bool ValidateFloorPlacement(Room room, Item item, int newX, int newY, bool onRoller, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        return HasValidTargetTiles(room, item, newX, newY, affectedTiles) &&
               (onRoller || HasOpenPlacementTiles(room, item, affectedTiles)) &&
               (onRoller || HasNoPlacementUserBlocking(room, item, affectedTiles));
    }

    public bool TryResolveFloorPlacement(Room room, Item item, int newX, int newY, int newRot, bool onRoller, double height, Dictionary<int, ThreeDCoord> affectedTiles, List<Item> itemsOnTile, out int resolvedRotation, out double resolvedZ)
    {
        resolvedRotation = NormalizeFloorItemRotation(item, newRot);
        resolvedZ = height != -1 ? height : room.GetGameMap().Model.SqFloorHeight[newX, newY];
        if (height != -1)
            return true;

        var itemsComplete = GetAffectedPlacementItems(room, newX, newY, affectedTiles, itemsOnTile);
        if (!onRoller && !AreStackedItemsPlaceable(item, itemsComplete))
            return false;

        resolvedZ = ResolveFloorPlacementHeight(item, newX, newY, resolvedRotation, resolvedZ, itemsComplete);
        return true;
    }

    public bool CheckPosItem(Room room, Item item, int newX, int newY, int newRot)
    {
        try
        {
            var affectedTiles = Gamemap.GetAffectedTiles(item.Definition.Length, item.Definition.Width, newX, newY, newRot);
            if (!HasValidCheckPositionTiles(room, newX, newY, affectedTiles))
                return false;
            if (IntersectsDoorTile(room, newX, newY, affectedTiles))
                return false;
            if (!HasMatchingBaseHeight(room, item, newX, newY, newRot))
                return false;
            if (!HasOpenCheckPositionTiles(room, newX, newY, affectedTiles))
                return false;
            if (!HasNoBlockingUsers(room, item, newX, newY, affectedTiles))
                return false;

            return HasOnlyStackableItems(room, item, newX, newY, affectedTiles);
        }
        catch
        {
            return false;
        }
    }

    private static bool HasValidTargetTiles(Room room, Item item, int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        if (!room.GetGameMap().ValidTile(newX, newY) || (room.GetGameMap().SquareHasUsers(newX, newY) && !item.Definition.IsSeat))
            return false;

        foreach (var tile in affectedTiles.Values)
        {
            if (!room.GetGameMap().ValidTile(tile.X, tile.Y) ||
                (room.GetGameMap().SquareHasUsers(tile.X, tile.Y) && !item.Definition.IsSeat))
                return false;
        }

        return true;
    }

    private static bool HasOpenPlacementTiles(Room room, Item item, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        foreach (var tile in affectedTiles.Values)
        {
            if (room.GetGameMap().Model.SqState[tile.X, tile.Y] != SquareState.Open && !item.Definition.IsSeat)
                return false;
        }

        return true;
    }

    private static bool HasNoPlacementUserBlocking(Room room, Item item, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        if (item.Definition.IsSeat || item.IsRoller)
            return true;

        foreach (var tile in affectedTiles.Values)
        {
            if (room.GetGameMap().GetRoomUsers(new(tile.X, tile.Y)).Count > 0)
                return false;
        }

        return true;
    }

    private static List<Item> GetAffectedPlacementItems(Room room, int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles, List<Item> itemsOnTile)
    {
        var itemsAffected = new List<Item>();
        foreach (var tile in affectedTiles.Values)
        {
            var coordinatedItems = room.GetGameMap().GetCoordinatedItems(new(tile.X, tile.Y));
            if (coordinatedItems != null)
                itemsAffected.AddRange(coordinatedItems);
        }

        var itemsComplete = new List<Item>();
        itemsComplete.AddRange(itemsOnTile);
        itemsComplete.AddRange(itemsAffected);
        return itemsComplete;
    }

    private static bool AreStackedItemsPlaceable(Item item, List<Item> itemsComplete)
    {
        foreach (var stackedItem in itemsComplete)
        {
            if (stackedItem == null || stackedItem.Id == item.Id || stackedItem.Definition == null)
                continue;

            if (!stackedItem.Definition.Stackable)
                return false;
        }

        return true;
    }

    private static int NormalizeFloorItemRotation(Item item, int newRot)
    {
        if (newRot != 0 && newRot != 2 && newRot != 4 && newRot != 6 && newRot != 8 && !item.Definition.AllowsExtraRotation)
            return 0;

        return newRot;
    }

    private static double ResolveFloorPlacementHeight(Item item, int newX, int newY, int newRot, double baseZ, List<Item> itemsComplete)
    {
        var resolvedZ = baseZ;
        if (item.Rotation != newRot && item.GetX == newX && item.GetY == newY)
            resolvedZ = item.GetZ;

        foreach (var stackedItem in itemsComplete)
        {
            if (stackedItem == null || stackedItem.Id == item.Id)
                continue;

            if (stackedItem.Definition.IsStacktool)
            {
                resolvedZ = stackedItem.GetZ;
                break;
            }

            if (stackedItem.TotalHeight > resolvedZ)
                resolvedZ = stackedItem.TotalHeight;
        }

        return resolvedZ;
    }

    private static bool HasValidCheckPositionTiles(Room room, int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        if (!room.GetGameMap().ValidTile(newX, newY))
            return false;

        foreach (var coord in affectedTiles.Values)
        {
            if (!room.GetGameMap().ValidTile(coord.X, coord.Y))
                return false;
        }

        return true;
    }

    private static bool IntersectsDoorTile(Room room, int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        if (room.GetGameMap().Model.DoorX == newX && room.GetGameMap().Model.DoorY == newY)
            return true;

        foreach (var coord in affectedTiles.Values)
        {
            if (room.GetGameMap().Model.DoorX == coord.X && room.GetGameMap().Model.DoorY == coord.Y)
                return true;
        }

        return false;
    }

    private static bool HasMatchingBaseHeight(Room room, Item item, int newX, int newY, int newRot)
    {
        var floorHeight = room.GetGameMap().Model.SqFloorHeight[newX, newY];
        return item.Rotation != newRot || item.GetX != newX || item.GetY != newY || item.GetZ == floorHeight;
    }

    private static bool HasOpenCheckPositionTiles(Room room, int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        if (room.GetGameMap().Model.SqState[newX, newY] != SquareState.Open)
            return false;

        foreach (var coord in affectedTiles.Values)
        {
            if (room.GetGameMap().Model.SqState[coord.X, coord.Y] != SquareState.Open)
                return false;
        }

        return true;
    }

    private static bool HasNoBlockingUsers(Room room, Item item, int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        if (item.Definition.IsSeat)
            return true;

        if (room.GetGameMap().SquareHasUsers(newX, newY))
            return false;

        foreach (var coord in affectedTiles.Values)
        {
            if (room.GetGameMap().SquareHasUsers(coord.X, coord.Y))
                return false;
        }

        return true;
    }

    private static bool HasOnlyStackableItems(Room room, Item item, int newX, int newY, Dictionary<int, ThreeDCoord> affectedTiles)
    {
        var itemsOnTarget = new List<Item>();
        itemsOnTarget.AddRange(room.GetGameMap().GetCoordinatedItems(new(newX, newY)));

        foreach (var coord in affectedTiles.Values)
        {
            var coordinatedItems = room.GetGameMap().GetCoordinatedItems(new(coord.X, coord.Y));
            if (coordinatedItems != null)
                itemsOnTarget.AddRange(coordinatedItems);
        }

        foreach (var roomItem in itemsOnTarget)
        {
            if (roomItem.Id != item.Id && !roomItem.Definition.Stackable)
                return false;
        }

        return true;
    }
}
