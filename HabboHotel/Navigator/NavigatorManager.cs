using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Navigator.SavedSearches;

namespace Plus.HabboHotel.Navigator;

public sealed class NavigatorManager : INavigatorManager
{
    private sealed class NavigatorCategoryRow
    {
        public int Id { get; init; }
        public int Enabled { get; init; }
        public string Category { get; init; } = string.Empty;
        public string CategoryIdentifier { get; init; } = string.Empty;
        public string PublicName { get; init; } = string.Empty;
        public int RequiredRank { get; init; }
        public string ViewMode { get; init; } = string.Empty;
        public string CategoryType { get; init; } = string.Empty;
        public string SearchAllowance { get; init; } = string.Empty;
        public int OrderId { get; init; }
    }

    private sealed class NavigatorPublicRow
    {
        public uint RoomId { get; init; }
        public string Caption { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string ImageUrl { get; init; } = string.Empty;
        public int Enabled { get; init; }
    }

    private readonly IDatabase _database;
    private readonly ILogger<NavigatorManager> _logger;

    private readonly Dictionary<uint, FeaturedRoom> _featuredRooms;
    private readonly Dictionary<int, SearchResultList> _searchResultLists;
    private readonly Dictionary<int, TopLevelItem> _topLevelItems;

    public NavigatorManager(IDatabase database, ILogger<NavigatorManager> logger)
    {
        _database = database;
        _logger = logger;
        _topLevelItems = new();
        _searchResultLists = new();

        //Does this need to be dynamic?
        _topLevelItems.Add(1, new(1, "official_view", "", ""));
        _topLevelItems.Add(2, new(2, "hotel_view", "", ""));
        _topLevelItems.Add(3, new(3, "roomads_view", "", ""));
        _topLevelItems.Add(4, new(4, "myworld_view", "", ""));
        _featuredRooms = new();
    }

    public void Init()
    {
        if (_searchResultLists.Count > 0)
            _searchResultLists.Clear();
        if (_featuredRooms.Count > 0)
            _featuredRooms.Clear();
        using (var connection = _database.Connection())
        {
            foreach (var row in connection.Query<NavigatorCategoryRow>(
                         """
                         SELECT
                             `id` AS Id,
                             `enabled` AS Enabled,
                             `category` AS Category,
                             `category_identifier` AS CategoryIdentifier,
                             `public_name` AS PublicName,
                             `required_rank` AS RequiredRank,
                             `view_mode` AS ViewMode,
                             `category_type` AS CategoryType,
                             `search_allowance` AS SearchAllowance,
                             `order_id` AS OrderId
                         FROM `navigator_categories`
                         ORDER BY `id` ASC
                         """))
            {
                if (row.Enabled != 1)
                    continue;
                if (_searchResultLists.ContainsKey(row.Id))
                    continue;

                _searchResultLists.Add(row.Id,
                    new(row.Id, row.Category, row.CategoryIdentifier, row.PublicName,
                        true, -1, row.RequiredRank, NavigatorViewModeUtility.GetViewModeByString(row.ViewMode),
                        row.CategoryType, row.SearchAllowance, row.OrderId));
            }

            foreach (var row in connection.Query<NavigatorPublicRow>(
                         """
                         SELECT
                             `room_id` AS RoomId,
                             `caption` AS Caption,
                             `description` AS Description,
                             `image_url` AS ImageUrl,
                             `enabled` AS Enabled
                         FROM `navigator_publics`
                         ORDER BY `order_num` ASC
                         """))
            {
                if (row.Enabled != 1)
                    continue;
                if (_featuredRooms.ContainsKey(row.RoomId))
                    continue;

                _featuredRooms.Add(row.RoomId,
                    new((int)row.RoomId, row.Caption, row.Description, row.ImageUrl));
            }
        }
        _logger.LogInformation("Navigator -> LOADED");
    }

    public List<SearchResultList> GetCategoriessForSearch(string category) => _searchResultLists.Where(cat => cat.Value.Category == category).OrderBy(cat => cat.Value.OrderId).Select(cat => cat.Value).ToList();

    public IReadOnlyCollection<SearchResultList> GetResultByIdentifier(string category) => _searchResultLists.Where(cat => cat.Value.CategoryIdentifier == category).OrderBy(cat => cat.Value.OrderId).Select(cat => cat.Value).ToList();

    public IReadOnlyCollection<SearchResultList> FlatCategories => _searchResultLists.Where(cat => cat.Value.CategoryType == NavigatorCategoryType.Category).OrderBy(cat => cat.Value.OrderId).Select(cat => cat.Value).ToList();

    public IReadOnlyCollection<SearchResultList> EventCategories => _searchResultLists.Where(cat => cat.Value.CategoryType == NavigatorCategoryType.PromotionCategory).OrderBy(cat => cat.Value.OrderId).Select(cat => cat.Value).ToList();

    public IReadOnlyCollection<TopLevelItem> TopLevelItems => _topLevelItems.Values;

    public IReadOnlyCollection<SearchResultList> SearchResultLists => _searchResultLists.Values;

    public bool TryGetTopLevelItem(int id, out TopLevelItem topLevelItem)
    {
        if (_topLevelItems.TryGetValue(id, out var item))
        {
            topLevelItem = item;
            return true;
        }
        topLevelItem = null!;
        return false;
    }

    public bool TryGetSearchResultList(int id, out SearchResultList searchResultList)
    {
        if (_searchResultLists.TryGetValue(id, out var resultList))
        {
            searchResultList = resultList;
            return true;
        }
        searchResultList = null!;
        return false;
    }

    public bool TryGetFeaturedRoom(uint roomId, out FeaturedRoom publicRoom)
    {
        if (_featuredRooms.TryGetValue(roomId, out var featuredRoom))
        {
            publicRoom = featuredRoom;
            return true;
        }
        publicRoom = null!;
        return false;
    }

    public IReadOnlyCollection<FeaturedRoom> FeaturedRooms => _featuredRooms.Values;

    public async Task<Dictionary<int, SavedSearch>> LoadUserNavigatorPreferences(int userId)
    {
        using var connection = _database.Connection();
        return (await connection.QueryAsync<SavedSearch>("SELECT `id`,`filter`,`search_code` as search FROM `user_saved_searches` WHERE `user_id` = @userId", new { userId })).ToDictionary(search => search.Id);
    }

    public async Task SaveHomeRoom(Habbo habbo, uint roomId)
    {
        habbo.HomeRoom = roomId;

        if (!RoomFactory.TryGetData(roomId, out _))
            return;

        using var connection = _database.Connection();
        await connection.ExecuteAsync("UPDATE users SET home_room = @roomid WHERE id = @userid LIMIT 1", new { roomid = roomId, userid = habbo.Id });
    }
}
