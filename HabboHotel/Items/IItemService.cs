using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items;

public interface IItemService
{
    Task<bool> PlaceItem(GameClient session, Room room, uint itemId, string placementData);
    Task<bool> MoveItem(GameClient session, Room room, uint itemId, int x, int y, int rotation);
    Task<bool> MoveWallItem(GameClient session, Room room, uint itemId, string wallPosition);
    Task<bool> PickupItem(GameClient session, Room room, uint itemId);
    Task<bool> UseItem(GameClient session, Room room, uint itemId, int state);
}
