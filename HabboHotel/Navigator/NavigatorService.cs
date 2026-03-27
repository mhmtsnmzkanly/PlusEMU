using Dapper;
using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Communication.Packets.Outgoing.Navigator.New;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat.Filter;

namespace Plus.HabboHotel.Navigator;

internal class NavigatorService : INavigatorService
{
    private readonly INavigatorManager _navigatorManager;
    private readonly INavigatorQueryService _navigatorQueryService;
    private readonly IRoomManager _roomManager;
    private readonly IRoomFactory _roomFactory;
    private readonly IRoomAppender _roomAppender;
    private readonly IWordFilterManager _wordFilterManager;
    private readonly IDatabase _database;

    public NavigatorService(
        INavigatorManager navigatorManager,
        INavigatorQueryService navigatorQueryService,
        IRoomManager roomManager,
        IRoomFactory roomFactory,
        IRoomAppender roomAppender,
        IWordFilterManager wordFilterManager,
        IDatabase database)
    {
        _navigatorManager = navigatorManager;
        _navigatorQueryService = navigatorQueryService;
        _roomManager = roomManager;
        _roomFactory = roomFactory;
        _roomAppender = roomAppender;
        _wordFilterManager = wordFilterManager;
        _database = database;
    }

    public Task Initialize(GameClient session)
    {
        session.Send(new NavigatorMetaDataParserComposer(_navigatorManager.TopLevelItems));
        session.Send(new NavigatorLiftedRoomsComposer());
        session.Send(new NavigatorCollapsedCategoriesComposer());
        session.Send(new NavigatorPreferencesComposer());
        return Task.CompletedTask;
    }

    public async Task UpdateSettings(GameClient session, uint roomId)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        await _navigatorManager.SaveHomeRoom(habbo, roomId);
        session.Send(new NavigatorSettingsComposer(roomId));
    }

    public Task GetUserFlatCategories(GameClient session)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        session.Send(new UserFlatCatsComposer(_navigatorManager.FlatCategories, habbo.Rank));
        return Task.CompletedTask;
    }

    public Task GetEventCategories(GameClient session)
    {
        session.Send(new NavigatorFlatCatsComposer(_navigatorManager.EventCategories));
        return Task.CompletedTask;
    }

    public Task CanCreateRoom(GameClient session)
    {
        session.Send(new CanCreateRoomComposer(false, 150));
        return Task.CompletedTask;
    }

    public Task GetGuestRoom(GameClient session, uint roomId, bool enter, bool forward)
    {
        if (!_roomFactory.TryGetData(roomId, out var data))
            return Task.CompletedTask;

        session.Send(new GetGuestRoomResultComposer(session, data!, enter, forward));
        return Task.CompletedTask;
    }

    public Task Search(GameClient session, string category, string search)
    {
        ICollection<SearchResultList> categories = new List<SearchResultList>();
        if (!string.IsNullOrEmpty(search))
        {
            if (_navigatorManager.TryGetSearchResultList(0, out var queryResult))
                categories.Add(queryResult);
        }
        else
        {
            categories = _navigatorManager.GetCategoriessForSearch(category);
            if (categories.Count == 0)
            {
                categories = _navigatorManager.GetResultByIdentifier(category).ToList();
                if (categories.Count > 0)
                {
                    session.Send(new NavigatorSearchResultSetComposer(category, search, categories, session, _navigatorQueryService, _roomAppender, 2, 100));
                    return Task.CompletedTask;
                }
            }
        }

        session.Send(new NavigatorSearchResultSetComposer(category, search, categories, session, _navigatorQueryService, _roomAppender));
        return Task.CompletedTask;
    }

    public Task CreateFlat(GameClient session, string name, string description, string modelName, int category, int maxVisitors, int tradeSettings)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var rooms = _roomFactory.GetRoomsDataByOwnerSortByName(habbo.Id);
        if (rooms.Count >= 500)
        {
            session.Send(new CanCreateRoomComposer(true, 500));
            return Task.CompletedTask;
        }

        var filteredName = _wordFilterManager.CheckMessage(name);
        var filteredDescription = _wordFilterManager.CheckMessage(description);
        if (filteredName.Length is < 3 or > 25)
            return Task.CompletedTask;

        if (!_roomManager.TryGetModel(modelName, out var model))
            return Task.CompletedTask;

        if (!_navigatorManager.TryGetSearchResultList(category, out var searchResultList))
            category = 36;
        else if (searchResultList.CategoryType != NavigatorCategoryType.Category || searchResultList.RequiredRank > habbo.Rank)
            category = 36;

        if (maxVisitors is < 10 or > 25)
            maxVisitors = 10;
        if (tradeSettings is < 0 or > 2)
            tradeSettings = 0;

        var newRoom = _roomManager.CreateRoom(session, filteredName, filteredDescription, category, maxVisitors, tradeSettings, model);
        if (newRoom != null)
            session.Send(new FlatCreatedComposer(newRoom.Id, filteredName));

        habbo.Messenger?.NotifyChangesToFriends();
        return Task.CompletedTask;
    }

    public async Task AddFavouriteRoom(GameClient session, uint roomId)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!_roomFactory.TryGetData(roomId, out var data) || data == null)
            return;

        if (habbo.FavoriteRooms.Count >= 30 || habbo.FavoriteRooms.Contains(roomId))
            return;

        habbo.FavoriteRooms.Add(roomId);
        session.Send(new UpdateFavouriteRoomComposer(roomId, true));

        using var connection = _database.Connection();
        await connection.ExecuteAsync(
            "INSERT INTO `user_favorites` (`user_id`, `room_id`) VALUES (@userId, @roomId)",
            new { userId = habbo.Id, roomId });
    }

    public async Task RemoveFavouriteRoom(GameClient session, uint roomId)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        habbo.FavoriteRooms.Remove(roomId);
        session.Send(new UpdateFavouriteRoomComposer(roomId, false));

        using var connection = _database.Connection();
        await connection.ExecuteAsync(
            "DELETE FROM `user_favorites` WHERE `user_id` = @userId AND `room_id` = @roomId LIMIT 1",
            new { userId = habbo.Id, roomId });
    }

    public async Task EditRoomPromotion(GameClient session, uint roomId, string name, string description)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        var filteredName = _wordFilterManager.CheckMessage(name);
        var filteredDescription = _wordFilterManager.CheckMessage(description);

        if (!_roomFactory.TryGetData(roomId, out var data))
            return;
        if (data!.OwnerId != habbo.Id)
            return;
        if (data.Promotion == null)
        {
            session.SendNotification("Oops, it looks like there isn't a room promotion in this room?");
            return;
        }

        using var connection = _database.Connection();
        await connection.ExecuteAsync(
            "UPDATE `room_promotions` SET `title` = @title, `description` = @description WHERE `room_id` = @roomId LIMIT 1",
            new { title = filteredName, description = filteredDescription, roomId });

        if (!_roomManager.TryGetRoom(roomId, out var room))
            return;

        data.Promotion.Name = filteredName;
        data.Promotion.Description = filteredDescription;
        room.SendPacket(new RoomEventComposer(data, data.Promotion));
    }
}
