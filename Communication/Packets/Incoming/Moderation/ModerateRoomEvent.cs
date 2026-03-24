using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Communication.Packets.Outgoing.Rooms.Settings;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModerateRoomEvent : IPacketEvent
{
    private readonly IRoomManager _roomManager;
    private readonly IDatabase _database;

    public ModerateRoomEvent(IRoomManager roomManager, IDatabase database)
    {
        _roomManager = roomManager;
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var moderator = session.GetHabbo();
        if (moderator?.Permissions == null || !moderator.Permissions.HasRight("mod_tool"))
            return Task.CompletedTask;
        if (!_roomManager.TryGetRoom(packet.ReadUInt(), out var room) || room == null)
            return Task.CompletedTask;
        var setLock = packet.ReadInt() == 1;
        var setName = packet.ReadInt() == 1;
        var kickAll = packet.ReadInt() == 1;
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
        using (var dbClient = _database.GetQueryReactor())
        {
            if (setName && setLock)
            {
                dbClient.RunQuery(
                    $"UPDATE `rooms` SET `caption` = 'Inappropriate to Hotel Management', `description` = 'Inappropriate to Hotel Management', `tags` = '', `state` = '1' WHERE `id` = '{room.RoomId}' LIMIT 1");
            }
            else if (setName)
            {
                dbClient.RunQuery(
                    $"UPDATE `rooms` SET `caption` = 'Inappropriate to Hotel Management', `description` = 'Inappropriate to Hotel Management', `tags` = '' WHERE `id` = '{room.RoomId}' LIMIT 1");
            }
            else if (setLock)
                dbClient.RunQuery($"UPDATE `rooms` SET `state` = '1', `tags` = '' WHERE `id` = '{room.RoomId}' LIMIT 1");
        }
        room.SendPacket(new RoomSettingsSavedComposer(room.RoomId));
        room.SendPacket(new RoomInfoUpdatedComposer(room.RoomId));
        if (kickAll)
        {
            foreach (var roomUser in room.GetRoomUserManager().GetUserList().ToList())
            {
                if (roomUser == null || roomUser.IsBot)
                    continue;
                var client = roomUser.GetClient();
                var targetHabbo = client?.GetHabbo();
                if (targetHabbo == null || client == null)
                    continue;
                if (targetHabbo.Rank >= moderator.Rank || targetHabbo.Id == moderator.Id)
                    continue;
                room.GetRoomUserManager().RemoveUserFromRoom(client, true);
            }
        }
        return Task.CompletedTask;
    }
}
