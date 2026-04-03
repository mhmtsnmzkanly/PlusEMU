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
using Microsoft.Extensions.DependencyInjection;
using Dapper;
using System.Data;
using Plus.Core;

namespace Plus.HabboHotel.Rooms;

public class RoomManager : IRoomManager
{
    private sealed class RoomModelRow
    {
        public string Id { get; init; } = string.Empty;
        public int DoorX { get; init; }
        public int DoorY { get; init; }
        public double DoorZ { get; init; }
        public int DoorOrientation { get; init; }
        public string Heightmap { get; init; } = string.Empty;
        public int ClubOnly { get; init; }
        public int WallHeight { get; init; }
        public int Custom { get; init; }
    }

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
    private readonly IRoomItemTrackingService _roomItemTrackingService;
    private readonly IRoomRollerApplyService _roomRollerApplyService;
    private readonly IChatManager _chatManager;
    private readonly IBotManager _botManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAchievementService _achievementService;
    private readonly IQuestService _questService;
    private readonly ICacheManager _cacheManager;
    private readonly IRoomFactory _roomFactory;
    private readonly IGroupManager _groupManager;
    private readonly IItemTeleporterFinder _itemTeleporterFinder;
    private readonly IItemHopperFinder _itemHopperFinder;
    private readonly IBadgeManager _badgeManager;
    private readonly IUserDataFactory _userDataFactory;
    private readonly IServerStatusSignal _serverStatusSignal;

    private readonly object _roomLoadingSync;
    private readonly Dictionary<string, RoomModel> _roomModels;
    private readonly ConcurrentDictionary<uint, Room> _rooms;
    private DateTime _cycleLastExecution;


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
        IRoomItemTrackingService roomItemTrackingService,
        IRoomRollerApplyService roomRollerApplyService,
        IGameClientManager gameClientManager,
        IGroupManager groupManager,
        IServiceProvider serviceProvider,
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
        IServerStatusSignal serverStatusSignal,
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
        _roomItemTrackingService = roomItemTrackingService;
        _roomRollerApplyService = roomRollerApplyService;
        _clientManager = gameClientManager;
        _groupManager = groupManager;
        _serviceProvider = serviceProvider;
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
        _serverStatusSignal = serverStatusSignal;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _rooms = new();
        _roomModels = new();
        _roomLoadingSync = new();
    }

    public int Count => _rooms.Count;

    public void OnCycle()
    {
        var sinceLastTime = DateTime.Now - _cycleLastExecution;
        if (sinceLastTime.TotalMilliseconds < 500)
            return;

        _cycleLastExecution = DateTime.Now;
        foreach (var room in GetRoomsToCycle())
        {
            if (room == null || room.Unloaded)
                continue;

            if (room.IsCrashed)
            {
                UnloadRoom(room.RoomId);
                continue;
            }

            if (room.ProcessTask == null || room.ProcessTask.IsCompleted)
            {
                room.ProcessTask?.Dispose();
                room.ProcessTask = new(room.ProcessRoom);
                room.ProcessTask.Start();
                room.IsLagging = 0;
            }
            else
            {
                room.IsLagging++;
                if (room.IsLagging >= 30)
                {
                    room.IsCrashed = true;
                    UnloadRoom(room.RoomId);
                }
            }
            NotifyRoomStateChanged(room);
        }
    }

    private List<Room> GetRoomsToCycle() => _rooms.Values.ToList();

    public void LoadModels()
    {
        _roomModels.Clear();
        using var connection = _database.Connection();
        var models = connection.Query<RoomModelRow>(GetRoomModelProjectionSql("WHERE `custom` = '0'"));
        foreach (var model in models.Select(MapRoomModel))
        {
            _roomModels.Add(model.Id, model);
        }
    }

    public bool LoadModel(string id) => _roomModels.ContainsKey(id);
    public void ReloadModel(string id)
    {
        using var connection = _database.Connection();
        var model = connection.QuerySingleOrDefault<RoomModelRow>(
            $"{GetRoomModelProjectionSql("WHERE `id` = @id")} LIMIT 1",
            new { id });
        if (model != null)
        {
            _roomModels[id] = MapRoomModel(model);
        }
    }

    private static RoomModel MapRoomModel(RoomModelRow row)
    {
        return new RoomModel(
            row.Id,
            row.DoorX,
            row.DoorY,
            row.DoorZ,
            row.DoorOrientation,
            row.Heightmap,
            row.ClubOnly == 1,
            row.WallHeight,
            row.Custom == 1);
    }

    private static string GetRoomModelProjectionSql(string whereClause)
    {
        return
            $"""
            SELECT
                `id` AS `id`,
                `door_x` AS `doorX`,
                `door_y` AS `doorY`,
                `door_z` AS `doorZ`,
                `door_dir` AS `doorOrientation`,
                `heightmap` AS `heightmap`,
                `club_only` = '1' AS `clubOnly`,
                `wall_height` AS `wallHeight`,
                `custom` = '1' AS `custom`
            FROM `room_models`
            {whereClause}
            """;
    }

    public bool TryGetModel(string id, out RoomModel model) => _roomModels.TryGetValue(id, out model!);

    public bool TryGetRoom(uint roomId, out Room room) => _rooms.TryGetValue(roomId, out room!);

    public ICollection<Room> GetRooms() => _rooms.Values;

    public void UnloadRoom(uint roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var room) || room.IsDisposed || room.IsUnloading)
            return;

        room.BeginUnload();
        EvictRoomUsers(room);

        if (_rooms.TryRemove(roomId, out room))
        {
            _serverStatusSignal.MarkDirty();
            DisposeRoom(room);
        }
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

            instance.Init();

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

        _serverStatusSignal.MarkDirty();
        room = instance;
        return true;
    }

    private Room CreateRoomInstance(RoomData data)
    {
        return new Room(data, _clientManager, _database, _itemLoader, _roomItemPersistenceService, _roomItemPlacementValidatorService, _roomItemPlacementPersistenceService, _roomRollerService, _roomItemInventoryService, _roomItemUpdateQueueService, _roomItemLoadService, _roomItemRemovalService, _roomItemStateService, _roomItemPlacementApplyService, _roomItemTrackingService, _roomRollerApplyService, _groupManager, _serviceProvider.GetRequiredService<IRoomService>(), _chatManager, _botManager, _achievementService, _questService, _cacheManager, _languageManager, _itemTeleporterFinder, _itemHopperFinder, _badgeManager, _userDataFactory, this, _loggerFactory);
    }

    private static void DisposeRoom(Room room) => room.Dispose();

    public void NotifyRoomStateChanged(Room room)
    {
        if (room == null || room.IsDisposed || room.IsUnloading)
            return;

        if (room.CanUnload)
            UnloadRoom(room.RoomId);
    }

    private void EvictRoomUsers(Room room)
    {
        var users = room.GetRoomUserManager().GetRoomUsers().ToList();
        var roomService = _serviceProvider.GetRequiredService<IRoomService>();
        foreach (var user in users)
        {
            var client = user.GetClient();
            if (client != null)
            {
                _ = roomService.LeaveRoom(client);
                continue;
            }

            room.ForceRemoveHabboFromRuntime(user);
        }
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
        _serverStatusSignal.MarkDirty();
    }
}
