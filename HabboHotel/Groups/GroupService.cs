using Dapper;
using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Groups;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Permissions;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.Communication.Packets.Outgoing.Users;
using Plus.Core.Language;
using Plus.Core.Settings;
using Plus.Database;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.Cache.Type;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat.Filter;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Groups;

internal class GroupService : IGroupService
{
    private readonly IGroupManager _groupManager;
    private readonly IRoomManager _roomManager;
    private readonly IRoomFactory _roomFactory;
    private readonly IDatabase _database;
    private readonly ICacheManager _cacheManager;
    private readonly ISettingsManager _settingsManager;
    private readonly ILanguageManager _languageManager;
    private readonly IWordFilterManager _wordFilterManager;
    private readonly IGameClientManager _gameClientManager;

    public GroupService(
        IGroupManager groupManager,
        IRoomManager roomManager,
        IRoomFactory roomFactory,
        IDatabase database,
        ICacheManager cacheManager,
        ISettingsManager settingsManager,
        ILanguageManager languageManager,
        IWordFilterManager wordFilterManager,
        IGameClientManager gameClientManager)
    {
        _groupManager = groupManager;
        _roomManager = roomManager;
        _roomFactory = roomFactory;
        _database = database;
        _cacheManager = cacheManager;
        _settingsManager = settingsManager;
        _languageManager = languageManager;
        _wordFilterManager = wordFilterManager;
        _gameClientManager = gameClientManager;
    }

    public async Task JoinGroup(GameClient session, int groupId)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!_groupManager.TryGetGroup(groupId, out var group) || group == null)
            return;
        if (group.IsMember(habbo.Id) || group.IsAdmin(habbo.Id) || group.HasRequest(habbo.Id) && group.Type == GroupType.Private)
            return;

        var groups = _groupManager.GetGroupsForUser(habbo.Id);
        if (groups.Count >= 1500)
        {
            session.Send(new BroadcastMessageAlertComposer(_languageManager.Require("group.membership.limit_reached")));
            return;
        }

        group.AddMember(habbo.Id);

        using (var connection = _database.Connection())
        {
            if (group.Type == GroupType.Locked)
            {
                await connection.ExecuteAsync("INSERT INTO `group_requests` (user_id, group_id) VALUES (@uid, @gid)", new { gid = group.Id, uid = habbo.Id });
                var groupAdmins = _gameClientManager.GetClients
                    .Where(client => client?.GetHabbo() != null && group.IsAdmin(client.GetHabbo()!.Id))
                    .Cast<GameClient>()
                    .ToList();

                foreach (var client in groupAdmins)
                    client.Send(new GroupMembershipRequestedComposer(group.Id, habbo, 3));

                session.Send(new GroupInfoComposer(group, session, _roomFactory, _cacheManager));
                return;
            }

            await connection.ExecuteAsync("INSERT INTO `group_memberships` (user_id, group_id) VALUES (@uid, @gid)", new { gid = group.Id, uid = habbo.Id });
        }

        session.Send(new GroupFurniConfigComposer(_groupManager.GetGroupsForUser(habbo.Id), _groupManager));
        session.Send(new GroupInfoComposer(group, session, _roomFactory, _cacheManager));
        SendFavouriteGroupRefreshForHabbo(session, habbo);
    }

    public async Task AcceptMembership(GameClient session, int groupId, int userId)
    {
        var habbo = session.GetHabbo();
        var permissions = habbo?.Permissions;
        if (habbo == null || permissions == null)
            return;

        if (!_groupManager.TryGetGroup(groupId, out var group) || group == null)
            return;
        if (habbo.Id != group.CreatorId && !group.IsAdmin(habbo.Id) && !permissions.HasRight("fuse_group_accept_any"))
            return;
        if (!group.HasRequest(userId))
            return;

        var targetHabbo = _gameClientManager.GetClientByUserId(userId)?.GetHabbo();
        if (targetHabbo == null)
        {
            session.SendNotification(_languageManager.Require("user.not_found"));
            return;
        }

        group.HandleRequest(userId, true);
        using var connection = _database.Connection();
        await connection.ExecuteAsync("INSERT INTO group_memberships (user_id, group_id) VALUES (@uid, @gid)", new { gid = group.Id, uid = userId });
        await connection.ExecuteAsync("DELETE FROM group_requests WHERE user_id=@uid AND group_id=@gid LIMIT 1", new { gid = group.Id, uid = userId });

        session.Send(new GroupMemberUpdatedComposer(groupId, targetHabbo, 4));
    }

    public async Task DeclineMembership(GameClient session, int groupId, int userId)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!_groupManager.TryGetGroup(groupId, out var group) || group == null)
            return;
        if (habbo.Id != group.CreatorId && !group.IsAdmin(habbo.Id))
            return;
        if (!group.HasRequest(userId))
            return;

        group.HandleRequest(userId, false);
        using var connection = _database.Connection();
        await connection.ExecuteAsync("DELETE FROM group_requests WHERE user_id=@uid AND group_id=@gid LIMIT 1", new { gid = group.Id, uid = userId });

        session.Send(new UnknownGroupComposer(group.Id, userId));
    }

    public async Task SetFavourite(GameClient session, int groupId)
    {
        var habbo = session.GetHabbo();
        var habboStats = habbo?.HabboStats;
        if (habbo == null || habboStats == null || groupId == 0)
            return;

        if (!_groupManager.TryGetGroup(groupId, out var group) || group == null)
            return;

        habboStats.FavouriteGroupId = group.Id;
        using var connection = _database.Connection();
        await connection.ExecuteAsync(
            "UPDATE `user_statistics` SET `groupid` = @groupId WHERE `id` = @userId LIMIT 1",
            new { groupId = habboStats.FavouriteGroupId, userId = habbo.Id });

        if (habbo.TryGetCurrentRoom(out var currentRoom))
        {
            currentRoom.SendPacket(new RefreshFavouriteGroupComposer(habbo.Id));
            currentRoom.SendPacket(new HabboGroupBadgesComposer(group));
            var user = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
            if (user != null)
                currentRoom.SendPacket(new UpdateFavouriteGroupComposer(group, user.VirtualId));
        }
        else
        {
            session.Send(new RefreshFavouriteGroupComposer(habbo.Id));
        }
    }

    public async Task RemoveFavourite(GameClient session)
    {
        var habbo = session.GetHabbo();
        var habboStats = habbo?.HabboStats;
        if (habbo == null || habboStats == null)
            return;

        habboStats.FavouriteGroupId = 0;
        using var connection = _database.Connection();
        await connection.ExecuteAsync(
            "UPDATE `user_statistics` SET `groupid` = 0 WHERE `id` = @userId LIMIT 1",
            new { userId = habbo.Id });

        if (habbo.TryGetCurrentRoom(out var currentRoom))
        {
            var user = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
            if (user != null)
                currentRoom.SendPacket(new UpdateFavouriteGroupComposer(null, user.VirtualId));
            SendFavouriteGroupRefresh(currentRoom, habbo.Id);
            return;
        }

        session.Send(new RefreshFavouriteGroupComposer(habbo.Id));
    }

    public async Task GiveAdminRights(GameClient session, int groupId, int userId)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!_groupManager.TryGetGroup(groupId, out var group) || group == null)
            return;
        if (habbo.Id != group.CreatorId || !group.IsMember(userId))
            return;

        var targetHabbo = _gameClientManager.GetClientByUserId(userId)?.GetHabbo();
        if (targetHabbo == null)
        {
            session.SendNotification(_languageManager.Require("user.not_found"));
            return;
        }

        group.MakeAdmin(userId);
        using var connection = _database.Connection();
        await connection.ExecuteAsync("UPDATE group_memberships SET `rank` = '1' WHERE `user_id` = @uid AND `group_id` = @gid LIMIT 1", new { gid = group.Id, uid = userId });

        UpdateGroupAdminRoomPermissions(group, userId, true);
        session.Send(new GroupMemberUpdatedComposer(groupId, targetHabbo, 1));
    }

    public async Task TakeAdminRights(GameClient session, int groupId, int userId)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!_groupManager.TryGetGroup(groupId, out var group) || group == null)
            return;
        if (habbo.Id != group.CreatorId || !group.IsMember(userId))
            return;

        var targetHabbo = _gameClientManager.GetClientByUserId(userId)?.GetHabbo();
        if (targetHabbo == null)
        {
            session.SendNotification(_languageManager.Require("user.not_found"));
            return;
        }

        group.TakeAdmin(userId);
        using var connection = _database.Connection();
        await connection.ExecuteAsync("UPDATE group_memberships SET `rank` = '0' WHERE user_id = @uid AND group_id = @gid", new { gid = group.Id, uid = userId });

        UpdateGroupAdminRoomPermissions(group, userId, false);
        session.Send(new GroupMemberUpdatedComposer(groupId, targetHabbo, 2));
    }

    public async Task RemoveMember(GameClient session, int groupId, int userId)
    {
        var habbo = session.GetHabbo();
        var habboStats = habbo?.HabboStats;
        if (habbo == null || habboStats == null)
            return;

        if (!_groupManager.TryGetGroup(groupId, out var group) || group == null)
            return;

        if (userId == habbo.Id)
        {
            await RemoveSelfFromGroup(session, habbo, group, userId);
            return;
        }

        if (group.CreatorId != habbo.Id && !group.IsAdmin(habbo.Id))
            return;
        if (!group.IsMember(userId))
            return;
        if (group.IsAdmin(userId) && group.CreatorId != habbo.Id)
        {
            session.SendNotification(_languageManager.Require("group.admin.remove_creator_only"));
            return;
        }

        if (group.IsAdmin(userId))
            group.TakeAdmin(userId);
        if (group.IsMember(userId))
            group.DeleteMember(userId);

        using var connection = _database.Connection();
        await connection.ExecuteAsync("DELETE FROM group_memberships WHERE user_id=@uid AND group_id=@gid LIMIT 1", new { gid = group.Id, uid = userId });

        var members = new List<CachedUser>();
        foreach (var id in group.GetAllMembers)
        {
            var groupMember = _cacheManager.GenerateUser(id);
            if (groupMember != null && !members.Contains(groupMember))
                members.Add(groupMember);
        }

        var finishIndex = 14 < members.Count ? 14 : members.Count;
        session.Send(new GroupMembersComposer(
            group,
            members.Take(finishIndex).ToList(),
            members.Count,
            1,
            group.CreatorId == habbo.Id || group.IsAdmin(habbo.Id),
            0,
            ""));
    }

    public async Task UpdateSettings(GameClient session, int groupId, int type, int furniOptions)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!_groupManager.TryGetGroup(groupId, out var group) || group == null)
            return;
        if (group.CreatorId != habbo.Id)
            return;

        group.Type = type switch
        {
            1 => GroupType.Locked,
            2 => GroupType.Private,
            _ => GroupType.Open
        };

        using var connection = _database.Connection();
        if (group.Type != GroupType.Locked && group.GetRequests.Count > 0)
        {
            foreach (var userId in group.GetRequests.ToList())
                group.HandleRequest(userId, false);
            group.ClearRequests();
            await connection.ExecuteAsync("DELETE FROM group_requests WHERE group_id=@gid", new { gid = group.Id });
        }

        await connection.ExecuteAsync(
            "UPDATE `groups` SET `state` = @groupState, `admindeco` = @adminDeco WHERE `id` = @groupId LIMIT 1",
            new
            {
                groupState = group.Type == GroupType.Open ? "0" : group.Type == GroupType.Locked ? "1" : "2",
                adminDeco = furniOptions == 1 ? "1" : "0",
                groupId = group.Id
            });

        group.AdminOnlyDeco = furniOptions;
        if (_roomManager.TryGetRoom(group.RoomId, out var room))
        {
            foreach (var user in room.GetRoomUserManager().GetRoomUsers().ToList())
            {
                if (room.OwnerId == user.UserId || group.IsAdmin(user.UserId) || !group.IsMember(user.UserId))
                    continue;

                if (furniOptions == 1)
                {
                    user.RemoveStatus("flatctrl 1");
                    user.UpdateNeeded = true;
                    user.GetClient()?.Send(new YouAreControllerComposer(0));
                }
                else if (furniOptions == 0 && !user.Statusses.ContainsKey("flatctrl 1"))
                {
                    user.SetStatus("flatctrl 1");
                    user.UpdateNeeded = true;
                    user.GetClient()?.Send(new YouAreControllerComposer(1));
                }
            }
        }

        session.Send(new GroupInfoComposer(group, session, _roomFactory, _cacheManager));
    }

    public async Task UpdateIdentity(GameClient session, int groupId, string name, string description)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!_groupManager.TryGetGroup(groupId, out var group) || group == null)
            return;
        if (group.CreatorId != habbo.Id)
            return;

        var filteredName = _wordFilterManager.CheckMessage(name);
        var filteredDescription = _wordFilterManager.CheckMessage(description);
        using var connection = _database.Connection();
        await connection.ExecuteAsync(
            "UPDATE `groups` SET `name` = @name, `desc` = @description WHERE `id` = @groupId LIMIT 1",
            new { name = filteredName, description = filteredDescription, groupId });

        group.Name = filteredName;
        group.Description = filteredDescription;
        session.Send(new GroupInfoComposer(group, session, _roomFactory, _cacheManager));
    }

    public async Task UpdateBadge(GameClient session, int groupId, IReadOnlyCollection<(int baseId, int firstPart, int secondPart)> parts)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!_groupManager.TryGetGroup(groupId, out var group) || group == null)
            return;
        if (group.CreatorId != habbo.Id)
            return;

        var badge = string.Empty;
        var index = 0;
        foreach (var (baseId, firstPart, secondPart) in parts)
        {
            badge += BadgePartUtility.WorkBadgeParts(index == 0, baseId.ToString(), firstPart.ToString(), secondPart.ToString());
            index++;
        }

        group.Badge = string.IsNullOrWhiteSpace(badge) ? "b05114s06114" : badge;
        using var connection = _database.Connection();
        await connection.ExecuteAsync(
            "UPDATE `groups` SET `badge` = @badge WHERE `id` = @groupId LIMIT 1",
            new { badge = group.Badge, groupId = group.Id });

        session.Send(new GroupInfoComposer(group, session, _roomFactory, _cacheManager));
    }

    public async Task UpdateColours(GameClient session, int groupId, int mainColour, int secondaryColour)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!_groupManager.TryGetGroup(groupId, out var group) || group == null)
            return;
        if (group.CreatorId != habbo.Id)
            return;

        using var connection = _database.Connection();
        await connection.ExecuteAsync(
            "UPDATE `groups` SET `colour1` = @colour1, `colour2` = @colour2 WHERE `id` = @groupId LIMIT 1",
            new { colour1 = mainColour, colour2 = secondaryColour, groupId = group.Id });

        group.Colour1 = mainColour;
        group.Colour2 = secondaryColour;
        session.Send(new GroupInfoComposer(group, session, _roomFactory, _cacheManager));

        if (!habbo.TryGetCurrentRoom(out var currentRoom))
            return;

        foreach (var item in currentRoom.GetRoomItemHandler().GetFloor.ToList())
        {
            if (item?.Definition == null)
                continue;
            if (!item.Definition.IsGroupFurni)
                continue;

            currentRoom.SendPacket(new ObjectUpdateComposer(item));
        }
    }

    public async Task DeleteGroup(GameClient session, int groupId)
    {
        var habbo = session.GetHabbo();
        var permissions = habbo?.Permissions;
        if (habbo == null || permissions == null)
            return;

        if (!_groupManager.TryGetGroup(groupId, out var group) || group == null)
        {
            session.SendNotification(_languageManager.Require("group.not_found"));
            return;
        }
        if (group.CreatorId != habbo.Id && !permissions.HasRight("group_delete_override"))
        {
            session.SendNotification(_languageManager.Require("group.delete.owner_only"));
            return;
        }

        var memberLimit = _settingsManager.GetIntOrDefault("group.delete.member.limit", 0);
        if (group.MemberCount >= memberLimit && !permissions.HasRight("group_delete_limit_override"))
        {
            session.SendNotification(_languageManager.Format("group.delete.member_limit_exceeded", ("limit", memberLimit.ToString())));
            return;
        }

        if (!_roomManager.TryGetRoom(group.RoomId, out var room))
            return;
        if (!_roomFactory.TryGetData(group.RoomId, out _))
            return;

        room.Group = null;
        _groupManager.DeleteGroup(group.Id);

        using var connection = _database.Connection();
        await connection.ExecuteAsync("DELETE FROM `groups` WHERE `id` = @groupId", new { groupId = group.Id });
        await connection.ExecuteAsync("DELETE FROM `group_memberships` WHERE `group_id` = @groupId", new { groupId = group.Id });
        await connection.ExecuteAsync("DELETE FROM `group_requests` WHERE `group_id` = @groupId", new { groupId = group.Id });
        await connection.ExecuteAsync("UPDATE `rooms` SET `group_id` = 0 WHERE `group_id` = @groupId LIMIT 1", new { groupId = group.Id });
        await connection.ExecuteAsync("UPDATE `user_statistics` SET `groupid` = 0 WHERE `groupid` = @groupId LIMIT 1", new { groupId = group.Id });
        await connection.ExecuteAsync("DELETE FROM `items_groups` WHERE `group_id` = @groupId", new { groupId = group.Id });

        _roomManager.UnloadRoom(room.Id);
        session.SendNotification(_languageManager.Require("group.delete.success"));
    }

    public Task PurchaseGroup(GameClient session, string name, string description, uint roomId, int mainColour, int secondaryColour, IReadOnlyCollection<(int baseId, int firstPart, int secondPart)> parts)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var filteredName = _wordFilterManager.CheckMessage(name);
        var filteredDescription = _wordFilterManager.CheckMessage(description);
        var groupCost = _settingsManager.GetIntOrDefault("catalog.group.purchase.cost", 0);
        if (habbo.Credits < groupCost)
        {
            session.Send(new BroadcastMessageAlertComposer(_languageManager.Format(
                "group.purchase.not_enough_credits",
                ("cost", groupCost.ToString()),
                ("credits", habbo.Credits.ToString()))));
            return Task.CompletedTask;
        }

        if (!_roomFactory.TryGetData(roomId, out var room) || room == null || room.OwnerId != habbo.Id || room.Group != null)
            return Task.CompletedTask;

        var badge = string.Empty;
        var index = 0;
        foreach (var (baseId, firstPart, secondPart) in parts.Take(5))
        {
            badge += BadgePartUtility.WorkBadgeParts(index == 0, baseId.ToString(), firstPart.ToString(), secondPart.ToString());
            index++;
        }

        habbo.Credits -= groupCost;
        session.Send(new CreditBalanceComposer(habbo.Credits));

        if (!_groupManager.TryCreateGroup(habbo, filteredName, filteredDescription, roomId, badge, mainColour, secondaryColour, out var group))
        {
            session.SendNotification(_languageManager.Require("group.create.error"));
            return Task.CompletedTask;
        }

        session.Send(new PurchaseOkComposer());
        if (_roomManager.TryGetRoom(roomId, out var roomInstance))
            roomInstance.Group = group;
            
        if (roomInstance == null || !habbo.IsInRoom(roomInstance))
            session.Send(new RoomForwardComposer(roomId));
        session.Send(new NewGroupInfoComposer(roomId, group.Id));
        return Task.CompletedTask;
    }

    private async Task RemoveSelfFromGroup(GameClient session, Habbo habbo, Group group, int userId)
    {
        if (group.IsMember(userId))
            group.DeleteMember(userId);
        if (group.IsAdmin(userId))
        {
            group.TakeAdmin(userId);
            UpdateGroupAdminRoomPermissions(group, userId, false);
        }

        using var connection = _database.Connection();
        await connection.ExecuteAsync(
            "DELETE FROM `group_memberships` WHERE `group_id` = @groupId AND `user_id` = @userId",
            new { groupId = group.Id, userId });

        session.Send(new GroupInfoComposer(group, session, _roomFactory, _cacheManager));
        if (habbo.HabboStats?.FavouriteGroupId != group.Id)
            return;

        habbo.HabboStats.FavouriteGroupId = 0;
        await connection.ExecuteAsync(
            "UPDATE `user_statistics` SET `groupid` = '0' WHERE `id` = @userId LIMIT 1",
            new { userId });

        if (group.AdminOnlyDeco == 0)
            UpdateGroupControllerStatus(group, habbo.Id, false);

        if (habbo.TryGetCurrentRoom(out var currentRoom))
        {
            var user = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
            if (user != null)
                currentRoom.SendPacket(new UpdateFavouriteGroupComposer(group, user.VirtualId));
            SendFavouriteGroupRefresh(currentRoom, habbo.Id);
            return;
        }

        session.Send(new RefreshFavouriteGroupComposer(habbo.Id));
    }

    private static void SendFavouriteGroupRefresh(GameClient session, Habbo habbo) => session.Send(new RefreshFavouriteGroupComposer(habbo.Id));

    private static void SendFavouriteGroupRefresh(Room room, int habboId) => room.SendPacket(new RefreshFavouriteGroupComposer(habboId));

    private void SendFavouriteGroupRefreshForHabbo(GameClient session, Habbo habbo)
    {
        if (habbo.TryGetCurrentRoom(out var currentRoom))
        {
            SendFavouriteGroupRefresh(currentRoom, habbo.Id);
            return;
        }

        SendFavouriteGroupRefresh(session, habbo);
    }

    private static bool IsUserOutsideRoom(Habbo habbo, uint roomId)
    {
        if (!habbo.TryGetCurrentRoom(out var currentRoom))
            return true;

        return currentRoom.Id != roomId;
    }

    private static void NotifyControllerStatus(RoomUser user, int controllerLevel)
    {
        var habbo = user.GetClient()?.GetHabbo();
        if (habbo == null || !habbo.TryGetClient(out var client))
            return;

        client.Send(new YouAreControllerComposer(controllerLevel));
    }

    private void UpdateGroupAdminRoomPermissions(Group group, int userId, bool enabled)
    {
        if (!_roomManager.TryGetRoom(group.RoomId, out var room))
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(userId);
        if (user == null)
            return;

        if (enabled)
        {
            if (!user.Statusses.ContainsKey("flatctrl 3"))
                user.SetStatus("flatctrl 3");
            user.UpdateNeeded = true;
            NotifyControllerStatus(user, 3);
            return;
        }

        if (user.Statusses.ContainsKey("flatctrl 3"))
            user.RemoveStatus("flatctrl 3");
        user.UpdateNeeded = true;
        NotifyControllerStatus(user, 0);
    }

    private void UpdateGroupControllerStatus(Group group, int userId, bool enabled)
    {
        if (!_roomManager.TryGetRoom(group.RoomId, out var room))
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(userId);
        if (user == null)
            return;

        if (enabled)
        {
            if (!user.Statusses.ContainsKey("flatctrl 1"))
                user.SetStatus("flatctrl 1");
            user.UpdateNeeded = true;
            NotifyControllerStatus(user, 1);
            return;
        }

        user.RemoveStatus("flatctrl 1");
        user.UpdateNeeded = true;
        NotifyControllerStatus(user, 0);
    }
}
