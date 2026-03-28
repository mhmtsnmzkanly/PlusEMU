using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.PathFinding;

namespace Plus.HabboHotel.Rooms;

public interface IRoomItemPlacementApplyService
{
    bool ApplyFloorPlacement(Room room, GameClient session, Item item, int newX, int newY, int newRot, double newZ, bool newItem, bool onRoller, bool sendMessage, bool updateRoomUserStatuses, Dictionary<int, ThreeDCoord> affectedTiles, IDictionary<uint, Item> floorItems, IDictionary<uint, Item> wallItems, Action<Item> markItemUpdated);
    bool ApplyRollerFloorPlacement(Room room, Item item, int newX, int newY, double newZ, Action<Item> markItemUpdated);
    bool ApplyWallPlacement(Room room, GameClient session, Item item, IDictionary<uint, Item> floorItems, IDictionary<uint, Item> wallItems);
}
