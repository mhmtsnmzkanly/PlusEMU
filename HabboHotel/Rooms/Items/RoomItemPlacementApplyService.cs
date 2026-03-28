using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Core;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.PathFinding;

namespace Plus.HabboHotel.Rooms;

public class RoomItemPlacementApplyService : IRoomItemPlacementApplyService
{
    private readonly IRoomItemPlacementPersistenceService _roomItemPlacementPersistenceService;
    private readonly IRoomItemStateService _roomItemStateService;

    public RoomItemPlacementApplyService(IRoomItemPlacementPersistenceService roomItemPlacementPersistenceService, IRoomItemStateService roomItemStateService)
    {
        _roomItemPlacementPersistenceService = roomItemPlacementPersistenceService;
        _roomItemStateService = roomItemStateService;
    }

    public bool ApplyFloorPlacement(Room room, GameClient session, Item item, int newX, int newY, int newRot, double newZ, bool newItem, bool onRoller, bool sendMessage, bool updateRoomUserStatuses, Dictionary<int, ThreeDCoord> affectedTiles, IDictionary<uint, Item> floorItems, IDictionary<uint, Item> wallItems, Action<Item> markItemUpdated)
    {
        item.Rotation = newRot;
        item.SetState(newX, newY, newZ, affectedTiles);
        if (!onRoller && session != null)
            item.Interactor.OnPlace(session, item);

        if (newItem)
        {
            if (floorItems.ContainsKey(item.Id))
            {
                if (session != null)
                    session.SendNotification(PlusEnvironment.LanguageManager.TryGetValue("room.item.already_placed"));
                room.GetGameMap().RemoveFromMap(item);
                return true;
            }

            if (item.IsFloorItem && !floorItems.ContainsKey(item.Id))
                floorItems[item.Id] = item;
            else if (item.IsWallItem && !wallItems.ContainsKey(item.Id))
                wallItems[item.Id] = item;

            if (sendMessage)
                room.SendPacket(new ObjectAddComposer(item));
        }
        else
        {
            markItemUpdated(item);
            if (!onRoller && sendMessage)
                room.SendPacket(new ObjectUpdateComposer(item));
        }

        room.GetGameMap().AddToMap(item);
        if (item.Definition.IsSeat)
            updateRoomUserStatuses = true;
        if (updateRoomUserStatuses)
            room.GetRoomUserManager().UpdateUserStatusses();
        if (item.Definition.InteractionType == InteractionType.Tent || item.Definition.InteractionType == InteractionType.TentSmall)
        {
            room.RemoveTent(item.Id);
            room.AddTent(item.Id);
        }

        _roomItemPlacementPersistenceService.SaveFloorPlacement(room.RoomId, item);
        return true;
    }

    public bool ApplyRollerFloorPlacement(Room room, Item item, int newX, int newY, double newZ, Action<Item> markItemUpdated)
    {
        room.GetGameMap().RemoveFromMap(item);
        item.SetState(newX, newY, newZ, Gamemap.GetAffectedTiles(item.Definition.Length, item.Definition.Width, newX, newY, item.Rotation));
        _roomItemStateService.EnsureTonerData(room, item);
        markItemUpdated(item);
        room.GetGameMap().AddItemToMap(item);
        return true;
    }

    public bool ApplyWallPlacement(Room room, GameClient session, Item item, IDictionary<uint, Item> floorItems, IDictionary<uint, Item> wallItems)
    {
        if (!item.IsWallItem || wallItems.ContainsKey(item.Id))
            return false;

        if (floorItems.ContainsKey(item.Id))
        {
            session.SendNotification(PlusEnvironment.LanguageManager.TryGetValue("room.item.already_placed"));
            return true;
        }

        item.Interactor.OnPlace(session, item);
        _roomItemStateService.InitializeWallItemState(room, item);
        _roomItemPlacementPersistenceService.SaveWallPlacement(room.RoomId, item);
        wallItems[item.Id] = item;
        room.SendPacket(new ItemAddComposer(item));
        return true;
    }
}
