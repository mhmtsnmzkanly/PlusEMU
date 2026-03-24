using Dapper;
using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Permissions;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.Communication.Packets.Outgoing.Rooms.Settings;
using Plus.Core.Language;
using Plus.Database;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Navigator;

namespace Plus.HabboHotel.Rooms;

internal class RoomAccessService : IRoomAccessService
{
    private readonly ILanguageManager _languageManager;
    private readonly ICacheManager _cacheManager;
    private readonly IDatabase _database;
    private readonly IGameClientManager _clientManager;
    private readonly IAchievementManager _achievementManager;
    private readonly IRoomManager _roomManager;
    private readonly INavigatorManager _navigatorManager;

    public RoomAccessService(
        ILanguageManager languageManager,
        ICacheManager cacheManager,
        IDatabase database,
        IGameClientManager clientManager,
        IAchievementManager achievementManager,
        IRoomManager roomManager,
        INavigatorManager navigatorManager)
    {
        _languageManager = languageManager;
        _cacheManager = cacheManager;
        _database = database;
        _clientManager = clientManager;
        _achievementManager = achievementManager;
        _roomManager = roomManager;
        _navigatorManager = navigatorManager;
    }

    public Task AssignRights(Room room, GameClient session, int userId)
    {
        if (!room.CheckRights(session, true))
            return Task.CompletedTask;
        if (room.UsersWithRights.Contains(userId))
        {
            session.SendNotification(_languageManager.TryGetValue("room.rights.user.has_rights"));
            return Task.CompletedTask;
        }

        room.UsersWithRights.Add(userId);
        using (var connection = _database.Connection())
        {
            connection.Execute(
                "INSERT INTO `room_rights` (`room_id`,`user_id`) VALUES (@roomId, @userId)",
                new { roomId = room.RoomId, userId });
        }

        var roomUser = room.GetRoomUserManager().GetRoomUserByHabbo(userId);
        if (roomUser != null && !roomUser.IsBot)
        {
            roomUser.SetStatus("flatctrl 1");
            roomUser.UpdateNeeded = true;
            var targetClient = roomUser.GetClient();
            var targetHabbo = targetClient?.GetHabbo();
            targetClient?.Send(new YouAreControllerComposer(1));
            if (targetHabbo != null)
                session.Send(new FlatControllerAddedComposer(room.RoomId, targetHabbo.Id, targetHabbo.Username));
            return Task.CompletedTask;
        }

        var user = _cacheManager.GenerateUser(userId);
        if (user != null)
            session.Send(new FlatControllerAddedComposer(room.RoomId, user.Id, user.Username));

        return Task.CompletedTask;
    }

    public Task RemoveRights(Room room, GameClient session, IReadOnlyCollection<int> userIds)
    {
        if (!room.CheckRights(session, true))
            return Task.CompletedTask;

        foreach (var userId in userIds.Take(101))
        {
            if (userId <= 0 || !room.UsersWithRights.Contains(userId))
                continue;

            var user = room.GetRoomUserManager().GetRoomUserByHabbo(userId);
            if (user != null && !user.IsBot)
            {
                user.RemoveStatus("flatctrl 1");
                user.UpdateNeeded = true;
                user.GetClient()?.Send(new YouAreControllerComposer(0));
            }

            using (var connection = _database.Connection())
            {
                connection.Execute(
                    "DELETE FROM `room_rights` WHERE `user_id` = @uid AND `room_id` = @rid LIMIT 1",
                    new { uid = userId, rid = room.Id });
            }

            room.UsersWithRights.Remove(userId);
            session.Send(new FlatControllerRemovedComposer(room, userId));
        }

        return Task.CompletedTask;
    }

    public Task RemoveAllRights(Room room, GameClient session)
    {
        if (!room.CheckRights(session, true))
            return Task.CompletedTask;

        foreach (var userId in new List<int>(room.UsersWithRights))
        {
            var user = room.GetRoomUserManager().GetRoomUserByHabbo(userId);
            if (user != null && !user.IsBot)
            {
                user.RemoveStatus("flatctrl 1");
                user.UpdateNeeded = true;
                user.GetClient()?.Send(new YouAreControllerComposer(0));
            }

            using (var connection = _database.Connection())
            {
                connection.Execute(
                    "DELETE FROM `room_rights` WHERE `user_id` = @uid AND `room_id` = @rid LIMIT 1",
                    new { uid = userId, rid = room.Id });
            }

            session.Send(new FlatControllerRemovedComposer(room, userId));
            session.Send(new RoomRightsListComposer(room));
            session.Send(new UserUpdateComposer(room.GetRoomUserManager().GetUserList().ToList()));
        }

        room.UsersWithRights.Clear();
        return Task.CompletedTask;
    }

    public Task RemoveMyRights(Room room, GameClient session)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !room.CheckRights(session, false) || !room.UsersWithRights.Contains(habbo.Id))
            return Task.CompletedTask;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user != null && !user.IsBot)
        {
            user.RemoveStatus("flatctrl 1");
            user.UpdateNeeded = true;
            user.GetClient()?.Send(new YouAreNotControllerComposer());
        }

        using (var connection = _database.Connection())
        {
            connection.Execute(
                "DELETE FROM `room_rights` WHERE `user_id` = @uid AND `room_id` = @rid LIMIT 1",
                new { uid = habbo.Id, rid = room.Id });
        }

        room.UsersWithRights.Remove(habbo.Id);
        return Task.CompletedTask;
    }

    public Task LetUserIn(Room room, GameClient session, string username, bool accepted)
    {
        if (!room.CheckRights(session))
            return Task.CompletedTask;

        var client = _clientManager.GetClientByUsername(username);
        var habbo = client?.GetHabbo();
        if (client == null || habbo == null)
            return Task.CompletedTask;

        if (accepted)
        {
            habbo.RoomAuthOk = true;
            client.Send(new FlatAccessibleComposer(string.Empty));
            room.SendPacket(new FlatAccessibleComposer(habbo.Username), true);
            return Task.CompletedTask;
        }

        client.Send(new FlatAccessDeniedComposer(string.Empty));
        room.SendPacket(new FlatAccessDeniedComposer(habbo.Username), true);
        return Task.CompletedTask;
    }

    public Task UnbanUser(GameClient session, int userId, int roomId)
    {
        var room = session.GetHabbo()?.CurrentRoom;
        if (room == null || !room.CheckRights(session, true))
            return Task.CompletedTask;
        if (!room.GetBans().IsBanned(userId))
            return Task.CompletedTask;

        room.GetBans().Unban(userId);
        session.Send(new UnbanUserFromRoomComposer(roomId, userId));
        return Task.CompletedTask;
    }

    public Task GetBannedUsers(GameClient session)
    {
        var room = session.GetHabbo()?.CurrentRoom;
        if (room == null || !room.CheckRights(session, true))
            return Task.CompletedTask;
        if (room.GetBans().BannedUsers().Count > 0)
            session.Send(new GetRoomBannedUsersComposer(room));
        return Task.CompletedTask;
    }

    public Task ToggleMuteTool(GameClient session)
    {
        var room = session.GetHabbo()?.CurrentRoom;
        if (room == null || !room.CheckRights(session, true))
            return Task.CompletedTask;

        room.RoomMuted = !room.RoomMuted;
        foreach (var roomUser in room.GetRoomUserManager().GetRoomUsers().ToList())
        {
            roomUser?.GetClient()?.SendWhisper(room.RoomMuted ? "This room has been muted" : "This room has been unmuted");
        }

        room.SendPacket(new RoomMuteSettingsComposer(room.RoomMuted));
        return Task.CompletedTask;
    }

    public Task GetRoomFilterList(GameClient session)
    {
        var room = session.GetHabbo()?.CurrentRoom;
        if (room == null || !room.CheckRights(session))
            return Task.CompletedTask;

        session.Send(new GetRoomFilterListComposer(room));
        _achievementManager.ProgressAchievement(session, "ACH_SelfModRoomFilterSeen", 1);
        return Task.CompletedTask;
    }

    public Task ModifyRoomFilterList(GameClient session, bool added, string word)
    {
        var room = session.GetHabbo()?.CurrentRoom;
        if (room == null || !room.CheckRights(session))
            return Task.CompletedTask;

        if (added)
            room.GetFilter().AddFilter(word);
        else
            room.GetFilter().RemoveFilter(word);

        return Task.CompletedTask;
    }

    public Task SaveEnforcedCategorySettings(GameClient session, uint roomId, int categoryId, int tradeSettings)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;
        if (!_roomManager.TryGetRoom(roomId, out var room))
            return Task.CompletedTask;
        if (!room.CheckRights(session, true))
            return Task.CompletedTask;

        if (tradeSettings < 0 || tradeSettings > 2)
            tradeSettings = 0;
        if (!_navigatorManager.TryGetSearchResultList(categoryId, out var searchResultList))
            categoryId = 36;
        if (searchResultList.CategoryType != NavigatorCategoryType.Category || searchResultList.RequiredRank > habbo.Rank)
            categoryId = 36;

        return Task.CompletedTask;
    }
}
