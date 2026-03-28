using Dapper;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Navigator;

internal class NavigatorQueryService : INavigatorQueryService
{
    private readonly IDatabase _database;
    private readonly IRoomManager _roomManager;
    private readonly IRoomFactory _roomFactory;
    private readonly IGroupManager _groupManager;

    private sealed class UserIdRow
    {
        public int Id { get; init; }
    }

    public NavigatorQueryService(IDatabase database, IRoomManager roomManager, IRoomFactory roomFactory, IGroupManager groupManager)
    {
        _database = database;
        _roomManager = roomManager;
        _roomFactory = roomFactory;
        _groupManager = groupManager;
    }

    public ICollection<RoomData> GetSearchResults(SearchResultList result, string query, GameClient session, int limit)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Array.Empty<RoomData>();

        return result.CategoryType switch
        {
            NavigatorCategoryType.MyHistory => Array.Empty<RoomData>(),
            NavigatorCategoryType.Featured => Array.Empty<RoomData>(),
            NavigatorCategoryType.Query => QuerySearch(query),
            NavigatorCategoryType.Popular => _roomManager.GetPopularRooms(-1, limit).Cast<RoomData>().ToList(),
            NavigatorCategoryType.Recommended => _roomManager.GetRecommendedRooms(limit).Cast<RoomData>().ToList(),
            NavigatorCategoryType.Category => _roomManager.GetRoomsByCategory(result.Id, limit).Cast<RoomData>().ToList(),
            NavigatorCategoryType.MyRooms => _roomFactory.GetRoomsDataByOwnerSortByName(habbo.Id).OrderByDescending(x => x.UsersNow).ToList(),
            NavigatorCategoryType.MyFavourites => GetFavouriteRooms(habbo),
            NavigatorCategoryType.MyGroups => GetMyGroups(habbo.Id, limit),
            NavigatorCategoryType.MyFriendsRooms => GetMyFriendsRooms(habbo),
            NavigatorCategoryType.MyRights => GetMyRights(habbo.Id, limit),
            NavigatorCategoryType.TopPromotions => _roomManager.GetOnGoingRoomPromotions(16, limit).Cast<RoomData>().ToList(),
            NavigatorCategoryType.PromotionCategory => _roomManager.GetPromotedRooms(result.OrderId, limit).Cast<RoomData>().ToList(),
            _ => Array.Empty<RoomData>()
        };
    }

    private ICollection<RoomData> QuerySearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<RoomData>();

        if (query.StartsWith("owner:", StringComparison.OrdinalIgnoreCase))
            return GetRoomsByOwner(query[6..]);
        if (query.StartsWith("tag:", StringComparison.OrdinalIgnoreCase))
            return _roomManager.SearchTaggedRooms(query[4..]).Cast<RoomData>().ToList();
        if (query.StartsWith("group:", StringComparison.OrdinalIgnoreCase))
            return _roomManager.SearchGroupRooms(query[6..]).Cast<RoomData>().ToList();

        using var connection = _database.Connection();
        var roomIds = connection.Query<uint>(
            """
            SELECT `id`
            FROM `rooms`
            WHERE `caption` LIKE @query AND `state` != 'invisible'
            ORDER BY `users_now` DESC
            LIMIT 50
            """,
            new { query = $"{query}%" });
        return ResolveRooms(roomIds);
    }

    private ICollection<RoomData> GetRoomsByOwner(string username)
    {
        using var connection = _database.Connection();
        var userId = connection.QueryFirstOrDefault<int?>(
            "SELECT `id` FROM `users` WHERE `username` = @username LIMIT 1",
            new { username });
        if (userId is null or 0)
            return Array.Empty<RoomData>();

        var roomIds = connection.Query<uint>(
            """
            SELECT `id`
            FROM `rooms`
            WHERE `owner` = @userId AND `state` != 'invisible'
            ORDER BY `users_now` DESC
            LIMIT 50
            """,
            new { userId });
        return ResolveRooms(roomIds);
    }

    private ICollection<RoomData> GetFavouriteRooms(HabboHotel.Users.Habbo habbo)
    {
        var favourites = new List<RoomData>();
        foreach (var id in habbo.FavoriteRooms.ToArray())
        {
            uint roomId;
            if (id is uint uintRoomId)
                roomId = uintRoomId;
            else if (id is int intRoomId)
                roomId = (uint)intRoomId;
            else
                continue;
            if (!_roomFactory.TryGetData(roomId, out var data))
                continue;
            if (data != null && !favourites.Contains(data))
                favourites.Add(data);
        }

        return favourites;
    }

    private ICollection<RoomData> GetMyGroups(int userId, int limit)
    {
        var myGroups = new List<RoomData>();
        foreach (var group in _groupManager.GetGroupsForUser(userId).ToList())
        {
            if (!_roomFactory.TryGetData((uint)group.RoomId, out var data))
                continue;
            if (data != null && !myGroups.Contains(data))
                myGroups.Add(data);
        }

        return myGroups.Take(limit).ToList();
    }

    private ICollection<RoomData> GetMyFriendsRooms(HabboHotel.Users.Habbo habbo)
    {
        var roomIds = new List<uint>();
        if (habbo.Messenger == null)
            return Array.Empty<RoomData>();

        foreach (var buddy in habbo.Messenger.Friends.Values)
        {
            if (buddy == null || buddy.Id == habbo.Id || !buddy.TryGetCurrentRoom(out var room))
                continue;
            if (!roomIds.Contains(room.Id))
                roomIds.Add(room.Id);
        }

        return _roomManager.GetRoomsByIds(roomIds).Cast<RoomData>().ToList();
    }

    private ICollection<RoomData> GetMyRights(int userId, int limit)
    {
        using var connection = _database.Connection();
        var roomIds = connection.Query<uint>(
            "SELECT `room_id` FROM `room_rights` WHERE `user_id` = @userId LIMIT @fetchLimit",
            new { userId, fetchLimit = limit });
        return ResolveRooms(roomIds);
    }

    private ICollection<RoomData> ResolveRooms(IEnumerable<uint> roomIds)
    {
        var results = new List<RoomData>();
        foreach (var roomId in roomIds)
        {
            if (!_roomFactory.TryGetData(roomId, out var data))
                continue;
            if (data != null && !results.Contains(data))
                results.Add(data);
        }

        return results;
    }
}
