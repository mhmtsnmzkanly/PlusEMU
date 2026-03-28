using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.PathFinding;

namespace Plus.HabboHotel.Rooms;

public interface IRoomItemPlacementValidatorService
{
    bool CanPlaceNewFloorItem(Room room, Item item, bool newItem);
    bool HasConflictingRoller(Item item, List<Item> itemsOnTile);
    bool ValidateFloorPlacement(Room room, Item item, int newX, int newY, bool onRoller, Dictionary<int, ThreeDCoord> affectedTiles);
    bool TryResolveFloorPlacement(Room room, Item item, int newX, int newY, int newRot, bool onRoller, double height, Dictionary<int, ThreeDCoord> affectedTiles, List<Item> itemsOnTile, out int resolvedRotation, out double resolvedZ);
    bool CheckPosItem(Room room, Item item, int newX, int newY, int newRot);
}
