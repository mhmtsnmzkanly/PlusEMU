using Dapper;
using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Communication.Packets.Outgoing.Rooms.Settings;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Moderation;

internal class ModerationRoomService : IModerationRoomService
{
    private readonly IRoomManager _roomManager;
    private readonly IRoomService _roomService;
    private readonly IDatabase _database;

    public ModerationRoomService(IRoomManager roomManager, IRoomService roomService, IDatabase database)
    {
        _roomManager = roomManager;
        _roomService = roomService;
        _database = database;
    }

    public async Task ModerateRoom(GameClient session, uint roomId, bool setLock, bool setName, bool kickAll)
    {
        var moderator = session.GetHabbo();
        if (moderator?.Permissions == null || !moderator.Permissions.HasRight("mod_tool"))
            return;
        if (!_roomManager.TryGetRoom(roomId, out var room) || room == null)
            return;

        if (setName)
        {
            room.Name = "Inappropriate to Hotel Management";
            room.Description = "Inappropriate to Hotel Management";
        }

        if (setLock)
            room.Access = RoomAccess.Doorbell;

        if (room.Tags.Count > 0)
            room.ClearTags();

        if (room.HasActivePromotion)
            room.EndPromotion();

        using (var connection = _database.Connection())
        {
            if (setName && setLock)
            {
                connection.Execute(
                    "UPDATE `rooms` SET `caption` = @caption, `description` = @description, `tags` = '', `state` = '1' WHERE `id` = @roomId LIMIT 1",
                    new { caption = "Inappropriate to Hotel Management", description = "Inappropriate to Hotel Management", roomId = room.RoomId });
            }
            else if (setName)
            {
                connection.Execute(
                    "UPDATE `rooms` SET `caption` = @caption, `description` = @description, `tags` = '' WHERE `id` = @roomId LIMIT 1",
                    new { caption = "Inappropriate to Hotel Management", description = "Inappropriate to Hotel Management", roomId = room.RoomId });
            }
            else if (setLock)
            {
                connection.Execute(
                    "UPDATE `rooms` SET `state` = '1', `tags` = '' WHERE `id` = @roomId LIMIT 1",
                    new { roomId = room.RoomId });
            }
        }

        room.SendPacket(new RoomSettingsSavedComposer(room.RoomId));
        room.SendPacket(new RoomInfoUpdatedComposer(room.RoomId));

        if (!kickAll)
            return;

        foreach (var roomUser in room.GetRoomUserManager().GetUserList().ToList())
        {
            if (roomUser == null || roomUser.IsBot)
                continue;

            var client = roomUser.GetClient();
            var targetHabbo = client?.GetHabbo();
            if (client == null || targetHabbo == null)
                continue;
            if (targetHabbo.Rank >= moderator.Rank || targetHabbo.Id == moderator.Id)
                continue;

            await _roomService.LeaveRoom(client);
        }

        return;
    }
}
