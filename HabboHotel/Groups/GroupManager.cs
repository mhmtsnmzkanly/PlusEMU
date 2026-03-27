using System.Collections.Concurrent;
using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;
using Plus.Utilities;

namespace Plus.HabboHotel.Groups;

public class GroupManager : IGroupManager
{
    private sealed class GroupItemRow
    {
        public int Id { get; init; }
        public string Type { get; init; } = string.Empty;
        public string FirstValue { get; init; } = string.Empty;
        public string SecondValue { get; init; } = string.Empty;
    }

    private sealed class GroupRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Desc { get; init; } = string.Empty;
        public string Badge { get; init; } = string.Empty;
        public uint RoomId { get; init; }
        public int OwnerId { get; init; }
        public int Created { get; init; }
        public int State { get; init; }
        public int Colour1 { get; init; }
        public int Colour2 { get; init; }
        public int AdminDeco { get; init; }
        public int ForumEnabled { get; init; }
    }

    private readonly ILogger<GroupManager> _logger;
    private readonly IDatabase _database;
    private readonly IRoomFactory _roomFactory;
    private readonly Dictionary<int, GroupColours> _backgroundColours;
    private readonly List<GroupColours> _baseColours;

    private readonly List<GroupBadgeParts> _bases;

    private readonly object _groupLoadingSync;
    private readonly ConcurrentDictionary<int, Group> _groups;
    private readonly Dictionary<int, GroupColours> _symbolColours;
    private readonly List<GroupBadgeParts> _symbols;

    public GroupManager(ILogger<GroupManager> logger, IDatabase database, IRoomFactory roomFactory)
    {
        _logger = logger;
        _database = database;
        _roomFactory = roomFactory;
        _groupLoadingSync = new();
        _groups = new();
        _bases = new();
        _symbols = new();
        _baseColours = new();
        _symbolColours = new();
        _backgroundColours = new();
    }


    public ICollection<GroupBadgeParts> BadgeBases => _bases;

    public ICollection<GroupBadgeParts> BadgeSymbols => _symbols;

    public ICollection<GroupColours> BadgeBaseColours => _baseColours;

    public ICollection<GroupColours> BadgeSymbolColours => _symbolColours.Values;

    public ICollection<GroupColours> BadgeBackColours => _backgroundColours.Values;

    public void Init()
    {
        _bases.Clear();
        _symbols.Clear();
        _baseColours.Clear();
        _symbolColours.Clear();
        _backgroundColours.Clear();
        using var connection = _database.Connection();
        var groupItems = connection.Query<GroupItemRow>(
            """
            SELECT
                `id` AS Id,
                `type` AS Type,
                `firstvalue` AS FirstValue,
                `secondvalue` AS SecondValue
            FROM `groups_items`
            WHERE `enabled` = '1'
            """);

        foreach (var groupItem in groupItems)
        {
            switch (groupItem.Type)
            {
                case "base":
                    _bases.Add(new(groupItem.Id, groupItem.FirstValue, groupItem.SecondValue));
                    break;
                case "symbol":
                    _symbols.Add(new(groupItem.Id, groupItem.FirstValue, groupItem.SecondValue));
                    break;
                case "color":
                    _baseColours.Add(new(groupItem.Id, groupItem.FirstValue));
                    break;
                case "color2":
                    _symbolColours.Add(groupItem.Id, new(groupItem.Id, groupItem.FirstValue));
                    break;
                case "color3":
                    _backgroundColours.Add(groupItem.Id, new(groupItem.Id, groupItem.FirstValue));
                    break;
            }
        }
    }

    public bool TryGetGroup(int id, out Group group)
    {
        group = null!;
        if (_groups.ContainsKey(id))
            return _groups.TryGetValue(id, out group!);
        lock (_groupLoadingSync)
        {
            if (_groups.ContainsKey(id))
                return _groups.TryGetValue(id, out group!);
            using var connection = _database.Connection();
            var row = connection.QueryFirstOrDefault<GroupRow>(
                """
                SELECT
                    `id` AS Id,
                    `name` AS Name,
                    `desc` AS `Desc`,
                    `badge` AS Badge,
                    `room_id` AS RoomId,
                    `owner_id` AS OwnerId,
                    `created` AS Created,
                    `state` AS State,
                    `colour1` AS Colour1,
                    `colour2` AS Colour2,
                    `admindeco` AS AdminDeco,
                    `forum_enabled` AS ForumEnabled
                FROM `groups`
                WHERE `id` = @id
                LIMIT 1
                """,
                new { id });
            if (row != null)
            {
                group = new(
                    row.Id, row.Name, row.Desc, row.Badge, row.RoomId,
                    row.OwnerId,
                    row.Created, row.State, row.Colour1, row.Colour2, row.AdminDeco,
                    row.ForumEnabled == 1);
                group.InitMembers(connection);
                _groups.TryAdd(group.Id, group);
                return true;
            }
        }
        return false;
    }

    public bool TryCreateGroup(Habbo player, string name, string description, uint roomId, string badge, int colour1, int colour2, out Group @group)
    {
        group = new(0, name, description, badge, roomId, player.Id, (int)UnixTimestamp.GetNow(), 0, colour1, colour2, 0, false);
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(badge))
            return false;
        using var connection = _database.Connection();
        group.Id = Convert.ToInt32(connection.ExecuteScalar<long>(
            """
            INSERT INTO `groups` (`name`, `desc`, `badge`, `owner_id`, `created`, `room_id`, `state`, `colour1`, `colour2`, `admindeco`)
            VALUES (@name, @desc, @badge, @owner, UNIX_TIMESTAMP(), @room, '0', @colour1, @colour2, '0');
            SELECT LAST_INSERT_ID();
            """,
            new
            {
                name = group.Name,
                desc = group.Description,
                owner = group.CreatorId,
                badge = group.Badge,
                room = group.RoomId,
                colour1 = group.Colour1,
                colour2 = group.Colour2
            }));

        connection.Execute("INSERT INTO `group_memberships` (user_id, group_id, rank) VALUES (@uid, @gid, '1')", new { gid = group.Id, uid = player.Id });
        group.InitMembers(connection);
        
        if (!_groups.TryAdd(group.Id, group))
            return false;
        connection.Execute(
            "UPDATE `rooms` SET `group_id` = @gid WHERE `id` = @rid LIMIT 1",
            new { gid = group.Id, rid = group.RoomId });
        connection.Execute(
            "DELETE FROM `room_rights` WHERE `room_id` = @roomId",
            new { roomId });
        return true;
    }

    public string GetColourCode(int id, bool colourOne)
    {
        if (colourOne)
        {
            if (_symbolColours.ContainsKey(id)) return _symbolColours[id].Colour;
        }
        else
        {
            if (_backgroundColours.ContainsKey(id)) return _backgroundColours[id].Colour;
        }
        return "";
    }

    public void DeleteGroup(int id)
    {
        Group? group = null;
        if (_groups.ContainsKey(id))
            _groups.TryRemove(id, out group);
        if (group != null) group.Dispose();
    }

    public List<Group> GetGroupsForUser(int userId)
    {
        var groups = new List<Group>();
        using var connection = _database.Connection();
        var groupIds = connection.Query<int>(
            "SELECT g.id FROM `group_memberships` AS m RIGHT JOIN `groups` AS g ON m.group_id = g.id WHERE m.user_id = @user",
            new { user = userId });
        foreach (var groupId in groupIds)
        {
            if (TryGetGroup(groupId, out var group))
                groups.Add(group);
        }
        return groups;
    }

    public Dictionary<int, string> GetAllBadgesInRoom(Room room)
    {
        var badges = new Dictionary<int, string>();
        foreach (var groupIds in room.GetRoomUserManager().GetRoomUsers()
                     .Select(user =>
                     {
                         var client = user.GetClient();
                         var habbo = client?.GetHabbo();
                         return habbo?.HabboStats?.FavouriteGroupId ?? 0;
                     })
                     .Where(g => g > 0)
                     .Distinct())
        {
            if (!TryGetGroup(groupIds, out var group))
                continue;
            badges.Add(group.Id, group.Badge);
        }
        return badges;
    }
}
