using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.Core.Settings;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal class PlaceObjectEvent : RoomPacketEvent
{
    private readonly IRoomManager _roomManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IAchievementService _achievementService;

    public PlaceObjectEvent(IRoomManager roomManager, ISettingsManager settingsManager, IAchievementService achievementService)
    {
        _roomManager = roomManager;
        _settingsManager = settingsManager;
        _achievementService = achievementService;
    }

    /// TODO @80O: Unfuck this mess
    public override async Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var furniture = habbo?.Inventory?.Furniture;
        if (habbo?.Permissions == null || furniture == null)
            return;

        var rawData = packet.ReadString();
        var data = rawData.Split(' ');
        if (!uint.TryParse(data[0], out var itemId))
            return;
        var hasRights = room.CheckRights(session, false, true);
        if (!hasRights)
        {
            session.Send(new RoomNotificationComposer("furni_placement_error", "message", "${room.error.cant_set_not_owner}"));
            return;
        }
        if (room.GetRoomItemHandler().GetWallAndFloor.Count() > Convert.ToInt32(_settingsManager.TryGetValue("room.item.placement_limit")))
        {
            session.SendNotification($"You cannot have more than {Convert.ToInt32(_settingsManager.TryGetValue("room.item.placement_limit"))} items in a room!");
            return;
        }
        var inventoryItem = furniture.GetItem(itemId);
        if (inventoryItem == null)
            return;
        var item = inventoryItem.ToRoomObject();
        if (item == null)
            return;

        if (item.Definition.InteractionType == InteractionType.Exchange && room.OwnerId != habbo.Id && !habbo.Permissions.HasRight("room_item_place_exchange_anywhere"))
        {
            session.SendNotification("You cannot place exchange items in other people's rooms!");
            return;
        }

        //TODO: Make neat.
        switch (item.Definition.InteractionType)
        {
            case InteractionType.Moodlight:
            {
                var moodData = room.MoodlightData;
                if (moodData != null && room.GetRoomItemHandler().GetItem(moodData.ItemId) != null)
                {
                    session.SendNotification("You can only have one background moodlight per room!");
                    return;
                }
                break;
            }
            case InteractionType.Toner:
            {
                var tonerData = room.TonerData;
                if (tonerData != null && room.GetRoomItemHandler().GetItem(tonerData.ItemId) != null)
                {
                    session.SendNotification("You can only have one background toner per room!");
                    return;
                }
                break;
            }
            case InteractionType.Hopper:
            {
                if (room.GetRoomItemHandler().HopperCount > 0)
                {
                    session.SendNotification("You can only have one hopper per room!");
                    return;
                }
                break;
            }
            case InteractionType.Tent:
            case InteractionType.TentSmall:
            {
                room.AddTent(item.Id);
                break;
            }
        }
        if (!item.IsWallItem)
        {
            if (data.Length < 4)
                return;
            if (!int.TryParse(data[1], out var x)) return;
            if (!int.TryParse(data[2], out var y)) return;
            if (!int.TryParse(data[3], out var rotation)) return;
            if (room.GetRoomItemHandler().SetFloorItem(session, item, x, y, rotation, true, false, true))
            {
                furniture.RemoveItem(itemId);
                session.Send(new FurniListRemoveComposer(itemId));
                if (habbo.Id == room.OwnerId)
                    await _achievementService.ProgressAchievement(session, "ACH_RoomDecoFurniCount", 1);
                if (item.IsWired)
                {
                    try
                    {
                        room.GetWired().LoadWiredBox(item);
                    }
                    catch
                    {
                        Console.WriteLine(item.Definition.InteractionType);
                    }
                }
            }
            else
                session.Send(new RoomNotificationComposer("furni_placement_error", "message", "${room.error.cant_set_item}"));
        }
        else if (item.IsWallItem)
        {
            var correctedData = new string[data.Length - 1];
            for (var i = 1; i < data.Length; i++) correctedData[i - 1] = data[i];
            var wallPos = room.GetRoomItemHandler().WallPositionCheck(string.Join(" ", correctedData));
            if (wallPos != null)
            {
                item.WallCoordinates = wallPos;
                try
                {
                    if (room.GetRoomItemHandler().SetWallItem(session, item))
                    {
                        furniture.RemoveItem(itemId);
                        session.Send(new FurniListRemoveComposer(itemId));
                        if (habbo.Id == room.OwnerId)
                            await _achievementService.ProgressAchievement(session, "ACH_RoomDecoFurniCount", 1);
                    }
                }
                catch
                {
                    session.Send(new RoomNotificationComposer("furni_placement_error", "message", "${room.error.cant_set_item}"));
                }
            }
            else
                session.Send(new RoomNotificationComposer("furni_placement_error", "message", "${room.error.cant_set_item}"));
        }
    }
}
