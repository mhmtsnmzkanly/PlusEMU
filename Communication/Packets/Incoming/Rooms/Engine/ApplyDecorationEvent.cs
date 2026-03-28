using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Database;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal class ApplyDecorationEvent : RoomPacketEvent
{
    private readonly IAchievementService _achievementService;
    private readonly IQuestService _questService;
    private readonly IDatabase _database;

    public ApplyDecorationEvent(IAchievementService achievementService, IQuestService questService, IDatabase database)
    {
        _achievementService = achievementService;
        _questService = questService;
        _database = database;
    }

    public override async Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { Inventory.Furniture: { } furniture })
            return;
        if (!room.CheckRights(session, true))
            return;

        var item = furniture.GetItem(packet.ReadUInt());
        if (item == null || item.Definition == null)
            return;

        var decorationKey = string.Empty;
        switch (item.Definition.InteractionType)
        {
            case InteractionType.Floor: decorationKey = "floor"; break;
            case InteractionType.Wallpaper: decorationKey = "wallpaper"; break;
            case InteractionType.Landscape: decorationKey = "landscape"; break;
        }

        var data = (item.ExtraData is LegacyDataFormat legacyData ? legacyData.Data : string.Empty);
        if (string.IsNullOrWhiteSpace(data))
            return;

        switch (decorationKey)
        {
            case "floor": 
                room.Floor = data; 
                await _questService.ProgressUserQuest(session, QuestType.FurniDecoFloor); 
                await _achievementService.ProgressAchievement(session, "ACH_RoomDecoFloor", 1); 
                break;
            case "wallpaper": 
                room.Wallpaper = data; 
                await _questService.ProgressUserQuest(session, QuestType.FurniDecoWall); 
                await _achievementService.ProgressAchievement(session, "ACH_RoomDecoWallpaper", 1); 
                break;
            case "landscape": 
                room.Landscape = data; 
                await _achievementService.ProgressAchievement(session, "ACH_RoomDecoLandscape", 1); 
                break;
        }

        using var db = _database.Connection();
        // decorationKey is validated against enum values above (floor/wallpaper/landscape only) — column name is safe
        db.Execute($"UPDATE `rooms` SET `{decorationKey}` = @extradata WHERE `id` = @roomId LIMIT 1",
            new { extradata = data, roomId = room.RoomId });
        db.Execute("DELETE FROM `items` WHERE `id` = @id LIMIT 1", new { id = item.Id });
        furniture.RemoveItem(item.Id);
        session.Send(new FurniListRemoveComposer(item.Id));
        room.SendPacket(new RoomPropertyComposer(decorationKey, data));
    }
}
