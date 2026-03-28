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
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDatabase _database;
    private readonly ILanguageManager _languageManager;
    private readonly IGameClientManager _clientManager;
    private readonly IItemLoader _itemLoader;
    private readonly IRoomItemPersistenceService _roomItemPersistenceService;
    private readonly IRoomItemPlacementValidatorService _roomItemPlacementValidatorService;
    private readonly IRoomItemPlacementPersistenceService _roomItemPlacementPersistenceService;
    private readonly IRoomRollerService _roomRollerService;
    private readonly IRoomItemInventoryService _roomItemInventoryService;
    private readonly IRoomItemUpdateQueueService _roomItemUpdateQueueService;
    private readonly IRoomItemLoadService _roomItemLoadService;
    private readonly IRoomItemRemovalService _roomItemRemovalService;
    private readonly IRoomItemStateService _roomItemStateService;
    private readonly IRoomItemPlacementApplyService _roomItemPlacementApplyService;
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
        IRoomItemPersistenceService roomItemPersistenceService,
        IRoomItemPlacementValidatorService roomItemPlacementValidatorService,
        IRoomItemPlacementPersistenceService roomItemPlacementPersistenceService,
        IRoomRollerService roomRollerService,
        IRoomItemInventoryService roomItemInventoryService,
        IRoomItemUpdateQueueService roomItemUpdateQueueService,
        IRoomItemLoadService roomItemLoadService,
        IRoomItemRemovalService roomItemRemovalService,
        IRoomItemStateService roomItemStateService,
        IRoomItemPlacementApplyService roomItemPlacementApplyService,
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
        ILoggerFactory loggerFactory,
        ILogger<RoomManager> logger)
    {
        _database = database;
        _roomFactory = roomFactory;
        _itemLoader = itemLoader;
        _roomItemPersistenceService = roomItemPersistenceService;
        _roomItemPlacementValidatorService = roomItemPlacementValidatorService;
        _roomItemPlacementPersistenceService = roomItemPlacementPersistenceService;
        _roomRollerService = roomRollerService;
        _roomItemInventoryService = roomItemInventoryService;
        _roomItemUpdateQueueService = roomItemUpdateQueueService;
        _roomItemLoadService = roomItemLoadService;
        _roomItemRemovalService = roomItemRemovalService;
        _roomItemStateService = roomItemStateService;
        _roomItemPlacementApplyService = roomItemPlacementApplyService;
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
        _loggerFactory = loggerFactory;
        _logger = logger;
        _rooms = new();
        _roomModels = new();
        _roomLoadingSync = new();
    }

    public int Count => _rooms.Count;

    public void OnCycle()
    {
        var start = DateTime.Now;
        foreach (var room in GetRoomsToCycle())
            ProcessRoomCycle(room);

        LogSlowCycle(start);
    }

    private List<Room> GetRoomsToCycle() => _rooms.Values.ToList();

    private void ProcessRoomCycle(Room room)
    {
        if (room == null || room.Unloaded)
            return;

        room.UpdateLifecycleState();
        if (room.ShouldUnloadForInactivity())
        {
            UnloadRoom(room.RoomId);
            return;
        }

        if (room.HasUsers())
            room.OnCycle();
    }

    private void LogSlowCycle(DateTime start)
    {
        var span = DateTime.Now - start;
        if (span.TotalMilliseconds > 500)
            _logger.LogWarning("RoomManager.OnCycle took {span}ms to execute - Rooms lagging behind", span.TotalMilliseconds);
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
            DisposeRoom(room);
    }

    public bool TryLoadRoom(uint roomId, out Room room)
    {
        if (TryGetLoadedOrCreateRoom(roomId, out room))
            return true;

        return TryLoadRoomLocked(roomId, out room);
    }

    private bool TryGetLoadedRoom(uint roomId, out Room room) => _rooms.TryGetValue(roomId, out room!);

    private bool TryGetLoadedOrCreateRoom(uint roomId, out Room room) => TryGetLoadedRoom(roomId, out room);

    private bool TryLoadRoomLocked(uint roomId, out Room room)
    {
        lock (_roomLoadingSync)
        {
            if (TryGetLoadedRoom(roomId, out room))
                return true;

            if (!TryCreateRoomInstance(roomId, out var instance))
            {
                room = null!;
                return false;
            }

            return TryRegisterLoadedRoom(roomId, instance, out room);
        }
    }

    private bool TryCreateRoomInstance(uint roomId, out Room room)
    {
        room = null!;
        if (!_roomFactory.TryGetData(roomId, out var data) || data == null)
            return false;

        room = CreateRoomInstance(data);
        return true;
    }

    private bool TryRegisterLoadedRoom(uint roomId, Room instance, out Room room)
    {
        room = null!;
        if (!_rooms.TryAdd(roomId, instance))
            return false;

        room = instance;
        return true;
    }

    private Room CreateRoomInstance(RoomData data)
    {
        return new Room(data, _clientManager, _database, _itemLoader, _roomItemPersistenceService, _roomItemPlacementValidatorService, _roomItemPlacementPersistenceService, _roomRollerService, _roomItemInventoryService, _roomItemUpdateQueueService, _roomItemLoadService, _roomItemRemovalService, _roomItemStateService, _roomItemPlacementApplyService, _groupManager, _roomService, _chatManager, _botManager, _achievementService, _questService, _cacheManager, _languageManager, _itemTeleporterFinder, _itemHopperFinder, _badgeManager, _userDataFactory, this, _loggerFactory);
    }

    private static void DisposeRoom(Room room) => room.Dispose();

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
