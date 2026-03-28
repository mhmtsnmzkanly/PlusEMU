using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.Bots;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Quests;
using Plus.Core.Language;
using Plus.Database;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users.UserData;
using Microsoft.Extensions.Logging;
using Dapper;
using System.Data;

namespace Plus.HabboHotel.Rooms;

public class RoomManager : IRoomManager
{
    private readonly ILogger<RoomManager> _logger;
    private readonly IDatabase _database;
    private readonly ILanguageManager _languageManager;
    private readonly IGameClientManager _clientManager;
    private readonly IItemLoader _itemLoader;
    private readonly IChatManager _chatManager;
    private readonly IBotManager _botManager;
    private readonly IRoomService _roomService;
    private readonly IAchievementService _achievementService;
    private readonly IQuestService _questService;
    private readonly ICacheManager _cacheManager;
    private readonly IRoomFactory _roomFactory;
    private readonly IGroupManager _groupManager;
    private readonly IItemTeleporterFinder _itemTeleporterFinder;
    private readonly IItemHopperFinder _itemHopperFinder;
    private readonly IBadgeManager _badgeManager;
    private readonly IUserDataFactory _userDataFactory;

    private readonly object _roomLoadingSync;
    private readonly Dictionary<string, RoomModel> _roomModels;
    private readonly ConcurrentDictionary<uint, Room> _rooms;


    public RoomManager(IDatabase database,
        IRoomFactory roomFactory,
        IItemLoader itemLoader,
        IGameClientManager gameClientManager,
        IGroupManager groupManager,
        IRoomService roomService,
        IChatManager chatManager,
        IBotManager botManager,
        IAchievementService achievementService,
        IQuestService questService,
        ICacheManager cacheManager,
        ILanguageManager languageManager,
        IItemTeleporterFinder itemTeleporterFinder,
        IItemHopperFinder itemHopperFinder,
        IBadgeManager badgeManager,
        IUserDataFactory userDataFactory,
        ILogger<RoomManager> logger)
    {
        _database = database;
        _roomFactory = roomFactory;
        _itemLoader = itemLoader;
        _clientManager = gameClientManager;
        _groupManager = groupManager;
        _roomService = roomService;
        _chatManager = chatManager;
        _botManager = botManager;
        _achievementService = achievementService;
        _questService = questService;
        _cacheManager = cacheManager;
        _languageManager = languageManager;
        _itemTeleporterFinder = itemTeleporterFinder;
        _itemHopperFinder = itemHopperFinder;
        _badgeManager = badgeManager;
        _userDataFactory = userDataFactory;
        _logger = logger;
        _rooms = new();
        _roomModels = new();
        _roomLoadingSync = new();
    }

    public int Count => _rooms.Count;

    public void OnCycle()
    {
        var start = DateTime.Now;
        var roomsToCycle = _rooms.Values.ToList();
        foreach (var room in roomsToCycle)
        {
            if (room == null || room.Unloaded)
                continue;

            if (room.GetRoomUserManager().UserCount > 0)
                room.OnCycle();
            else if (room.IdleTime >= 60) // 1 minute
                UnloadRoom(room.RoomId);
            else
                room.IdleTime++;
        }
        var span = DateTime.Now - start;
        if (span.TotalMilliseconds > 500)
        {
            _logger.LogWarning("RoomManager.OnCycle took {span}ms to execute - Rooms lagging behind", span.TotalMilliseconds);
        }
    }

    public void LoadModels()
    {
        _roomModels.Clear();
        using var connection = _database.Connection();
        var models = connection.Query<RoomModel>("SELECT `id`, `door_x`, `door_y`, `door_z`, `door_dir`, `heightmap`, `public_room` = '1' as `is_public` FROM `room_models` WHERE `custom` = '0'");
        foreach (var model in models)
        {
            _roomModels.Add(model.Id, model);
        }
    }

    public bool LoadModel(string id) => _roomModels.ContainsKey(id);
    public void ReloadModel(string id)
    {
        using var connection = _database.Connection();
        var model = connection.QuerySingleOrDefault<RoomModel>("SELECT `id`, `door_x`, `door_y`, `door_z`, `door_dir`, `heightmap`, `public_room` = '1' as `is_public` FROM `room_models` WHERE `id` = @id LIMIT 1", new { id });
        if (model != null)
        {
            _roomModels[id] = model;
        }
    }

    public bool TryGetModel(string id, out RoomModel model) => _roomModels.TryGetValue(id, out model!);

    public bool TryGetRoom(uint roomId, out Room room) => _rooms.TryGetValue(roomId, out room!);

    public ICollection<Room> GetRooms() => _rooms.Values;

    public void UnloadRoom(uint roomId)
    {
        if (_rooms.TryRemove(roomId, out var room))
        {
            room.Dispose();
        }
    }

    public bool TryLoadRoom(uint roomId, out Room room)
    {
        if (_rooms.TryGetValue(roomId, out room!))
            return true;

        lock (_roomLoadingSync)
        {
            if (_rooms.TryGetValue(roomId, out room!))
                return true;

            if (!_roomFactory.TryGetData(roomId, out var data) || data == null)
            {
                room = null!;
                return false;
            }

            var myInstance = new Room(data, _clientManager, _database, _itemLoader, _groupManager, _roomService, _chatManager, _botManager, _achievementService, _questService, _cacheManager, _languageManager, _itemTeleporterFinder, _itemHopperFinder, _badgeManager, _userDataFactory, this);
            if (_rooms.TryAdd(roomId, myInstance))
            {
                room = myInstance;
                return true;
            }
        }

        room = null!;
        return false;
    }

    public List<Room> SearchGroupRooms(string query) => _rooms.Values.Where(x => x.Data.GroupId > 0 && x.Data.Name.Contains(query, System.StringComparison.OrdinalIgnoreCase)).ToList();
    public List<Room> SearchTaggedRooms(string query) => _rooms.Values.Where(x => x.Data.Tags.Any(t => t.Contains(query, System.StringComparison.OrdinalIgnoreCase))).ToList();
    public List<Room> GetPopularRooms(int category, int amount = 50) => _rooms.Values.Where(x => x.Data.Category == category).OrderByDescending(x => x.Data.UsersNow).Take(amount).ToList();
    public List<Room> GetRecommendedRooms(int amount = 50, int currentRoomId = 0) => _rooms.Values.Where(x => x.RoomId != currentRoomId).OrderByDescending(x => x.Data.UsersNow).Take(amount).ToList();
    public List<Room> GetPopularRatedRooms(int amount = 50) => _rooms.Values.OrderByDescending(x => x.Data.Score).Take(amount).ToList();
    public List<Room> GetRoomsByCategory(int category, int amount = 50) => _rooms.Values.Where(x => x.Data.Category == category).Take(amount).ToList();
    public List<Room> GetOnGoingRoomPromotions(int mode, int amount = 50) => _rooms.Values.Where(x => x.Data.Promotion != null).Take(amount).ToList();
    public List<Room> GetPromotedRooms(int categoryId, int amount = 50) => _rooms.Values.Where(x => x.Data.Promotion != null && (categoryId == -1 || x.Data.Promotion.CategoryId == categoryId)).Take(amount).ToList();
    public List<Room> GetGroupRooms(int amount = 50) => _rooms.Values.Where(x => x.Data.GroupId > 0).Take(amount).ToList();
    public List<Room> GetRoomsByIds(List<uint> ids, int amount = 50) => _rooms.Values.Where(x => ids.Contains(x.RoomId)).Take(amount).ToList();
    public Room TryGetRandomLoadedRoom() => _rooms.Values.OrderBy(_ => System.Guid.NewGuid()).FirstOrDefault()!;

    public RoomData CreateRoom(GameClient session, string name, string description, int category, int maxVisitors, int tradeSettings, RoomModel model, string wallpaper = "0.0", string floor = "0.0",
        string landscape = "0.0", int wallthick = 0, int floorthick = 0)
    {
        return _roomFactory.CreateRoomData(session, name, description, category, maxVisitors, tradeSettings, model, wallpaper, floor, landscape, wallthick, floorthick);
    }

    public void Dispose()
    {
        foreach (var room in _rooms.Values)
        {
            room.Dispose();
        }
        _rooms.Clear();
    }
}
