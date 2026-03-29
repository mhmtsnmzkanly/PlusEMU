using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.Core.Settings;
using Plus.Database;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items;

public class ItemService : IItemService
{
    private readonly IDatabase _database;
    private readonly IGameClientManager _clientManager;
    private readonly IAchievementService _achievementService;
    private readonly IQuestService _questService;
    private readonly ISettingsManager _settingsManager;

    public ItemService(IDatabase database,
        IGameClientManager clientManager,
        IAchievementService achievementService,
        IQuestService questService,
        ISettingsManager settingsManager)
    {
        _database = database;
        _clientManager = clientManager;
        _achievementService = achievementService;
        _questService = questService;
        _settingsManager = settingsManager;
    }

    public async Task<bool> PlaceItem(GameClient session, Room room, uint itemId, string placementData)
    {
        var habbo = session.GetHabbo();
        var furniture = habbo?.Inventory?.Furniture;
        if (habbo?.Permissions == null || furniture == null)
            return false;

        var inventoryItem = furniture.GetItem(itemId);
        if (inventoryItem == null)
            return false;

        var hasRights = room.CheckRights(session, false, true);
        if (!hasRights)
        {
            session.Send(new RoomNotificationComposer("furni_placement_error", "message", "${room.error.cant_set_not_owner}"));
            return false;
        }

        if (room.GetRoomItemHandler().GetWallAndFloor.Count() >= Convert.ToInt32(_settingsManager.TryGetValue("room.item.placement_limit")))
        {
            session.SendNotification($"You cannot have more than {Convert.ToInt32(_settingsManager.TryGetValue("room.item.placement_limit"))} items in a room!");
            return false;
        }

        var item = inventoryItem.ToRoomObject();
        if (item == null)
            return false;

        if (item.Definition.IsExchange && room.OwnerId != habbo.Id && !habbo.Permissions.HasRight("room_item_place_exchange_anywhere"))
        {
            session.SendNotification("You cannot place exchange items in other people's rooms!");
            return false;
        }

        // Interaction specific checks
        switch (item.Definition.InteractionType)
        {
            case var _ when item.Definition.IsMoodlight:
                if (room.MoodlightData != null && room.GetRoomItemHandler().GetItem(room.MoodlightData.ItemId) != null)
                {
                    session.SendNotification("You can only have one background moodlight per room!");
                    return false;
                }
                break;
            case var _ when item.Definition.IsToner:
                if (room.TonerData != null && room.GetRoomItemHandler().GetItem(room.TonerData.ItemId) != null)
                {
                    session.SendNotification("You can only have one background toner per room!");
                    return false;
                }
                break;
            case var _ when item.Definition.IsHopper:
                if (room.GetRoomItemHandler().HopperCount > 0)
                {
                    session.SendNotification("You can only have one hopper per room!");
                    return false;
                }
                break;
            case var _ when item.Definition.IsTent:
                room.AddTent(item.Id);
                break;
        }

        var data = placementData.Split(' ');
        if (!item.IsWallItem)
        {
            if (data.Length < 4) return false;
            if (!int.TryParse(data[1], out var x)) return false;
            if (!int.TryParse(data[2], out var y)) return false;
            if (!int.TryParse(data[3], out var rotation)) return false;

            if (room.GetRoomItemHandler().SetFloorItem(session, item, x, y, rotation, true, false, true))
            {
                furniture.RemoveItem(itemId);
                session.Send(new FurniListRemoveComposer(itemId));
                if (habbo.Id == room.OwnerId)
                    await _achievementService.ProgressAchievement(session, "ACH_RoomDecoFurniCount", 1);
                
                if (item.IsWired)
                    room.GetWired().LoadWiredBox(item);
                
                return true;
            }
            session.Send(new RoomNotificationComposer("furni_placement_error", "message", "${room.error.cant_set_item}"));
        }
        else
        {
            var correctedData = data.Skip(1).ToArray();
            var wallPos = room.GetRoomItemHandler().WallPositionCheck(string.Join(" ", correctedData));
            if (wallPos != null)
            {
                item.WallCoordinates = wallPos;
                if (room.GetRoomItemHandler().SetWallItem(session, item))
                {
                    furniture.RemoveItem(itemId);
                    session.Send(new FurniListRemoveComposer(itemId));
                    if (habbo.Id == room.OwnerId)
                        await _achievementService.ProgressAchievement(session, "ACH_RoomDecoFurniCount", 1);
                    return true;
                }
            }
            session.Send(new RoomNotificationComposer("furni_placement_error", "message", "${room.error.cant_set_item}"));
        }

        return false;
    }

    public async Task<bool> MoveItem(GameClient session, Room room, uint itemId, int x, int y, int rotation)
    {
        var item = room.GetRoomItemHandler().GetItem(itemId);
        if (item == null) return false;

        if (!room.CheckRights(session, false, true))
        {
            session.Send(new ObjectUpdateComposer(item));
            return false;
        }

        if (x != item.GetX || y != item.GetY)
            await _questService.ProgressUserQuest(session, QuestType.FurniMove);
        if (rotation != item.Rotation)
            await _questService.ProgressUserQuest(session, QuestType.FurniRotate);

        if (!room.GetRoomItemHandler().SetFloorItem(session, item, x, y, rotation, false, false, true))
        {
            room.SendPacket(new ObjectUpdateComposer(item));
            return false;
        }

        if (item.GetZ >= 0.1)
            await _questService.ProgressUserQuest(session, QuestType.FurniStack);

        return true;
    }

    public async Task<bool> MoveWallItem(GameClient session, Room room, uint itemId, string wallPosition)
    {
        var item = room.GetRoomItemHandler().GetItem(itemId);
        if (item == null || !item.IsWallItem) return false;

        if (!room.CheckRights(session, false, true))
            return false;

        var wallPos = room.GetRoomItemHandler().WallPositionCheck(wallPosition);
        if (wallPos == null) return false;

        item.WallCoordinates = wallPos;

        using var connection = _database.Connection();
        await connection.ExecuteAsync("UPDATE `items` SET `wall_pos` = @wallPos WHERE `id` = @id LIMIT 1", new { wallPos = item.WallCoordinates, id = item.Id });

        room.SendPacket(new ItemUpdateComposer(item));
        return true;
    }

    public async Task<bool> PickupItem(GameClient session, Room room, uint itemId)
    {
        var habbo = session.GetHabbo();
        var furniture = habbo?.Inventory?.Furniture;
        if (habbo?.Permissions == null || furniture == null)
            return false;

        var item = room.GetRoomItemHandler().GetItem(itemId);
        if (item == null) return false;

        if (item.Definition.IsPostIt) return false;

        var itemRights = item.UserId == habbo.Id || room.CheckRights(session, false) || (room.Group != null && room.CheckRights(session, false, true)) || habbo.Permissions.HasRight("room_item_take");

        if (!itemRights) return false;

        using var connection = _database.Connection();
        if (item.Definition.IsTent)
            room.RemoveTent(item.Id);
        
        if (item.Definition.IsMoodlight)
            await connection.ExecuteAsync("DELETE FROM `room_items_moodlight` WHERE `item_id` = @id LIMIT 1", new { id = item.Id });
        else if (item.Definition.IsToner)
            await connection.ExecuteAsync("DELETE FROM `room_items_toner` WHERE `id` = @id LIMIT 1", new { id = item.Id });

        if (item.UserId == habbo.Id || habbo.Permissions.HasRight("room_item_take"))
        {
            room.GetRoomItemHandler().RemoveFurniture(session, item.Id);
            furniture.AddItem(item.ToInventoryItem());
            session.Send(new FurniListUpdateComposer());
        }
        else // Ejected
        {
            var targetClient = _clientManager.GetClientByUserId(item.UserId);
            var targetFurniture = targetClient?.GetHabbo()?.Inventory?.Furniture;
            if (targetClient != null && targetFurniture != null)
            {
                room.GetRoomItemHandler().RemoveFurniture(targetClient, item.Id);
                targetFurniture.AddItem(item.ToInventoryItem());
                targetClient.Send(new FurniListUpdateComposer());
            }
            else
            {
                room.GetRoomItemHandler().RemoveFurniture(null, item.Id);
            }
        }

        await connection.ExecuteAsync("UPDATE `items` SET `room_id` = '0' WHERE `id` = @id LIMIT 1", new { id = item.Id });
        await _questService.ProgressUserQuest(session, QuestType.FurniPick);

        return true;
    }

    public async Task<bool> UseItem(GameClient session, Room room, uint itemId, int state)
    {
        var item = room.GetRoomItemHandler().GetItem(itemId);
        if (item == null) return false;

        // Interaction logic would go here if extracted from item.Interactor.OnTrigger
        // For now, most interactor logic is still inside Interactor classes.
        return true;
    }
}
