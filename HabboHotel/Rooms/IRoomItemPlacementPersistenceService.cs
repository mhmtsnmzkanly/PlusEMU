using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public interface IRoomItemPlacementPersistenceService
{
    void SaveFloorPlacement(uint roomId, Item item);
    void SaveWallPlacement(uint roomId, Item item);
}
