using System.Collections.Concurrent;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.Bots;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.Chat;
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

    private DateTime _cycleLastExecution;

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

            if (room.GetRoomUserManager().GetUserCount() > 0)
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

            var data = _roomFactory.CreateRoomData(roomId);
            if (data == null)
            {
                room = null!;
                return false;
            }

            var myInstance = new Room(data, _clientManager, _database, _itemLoader, _groupManager, _roomService, _chatManager, _botManager, _achievementService, _questService, _cacheManager, _languageManager, _itemTeleporterFinder, _itemHopperFinder, _badgeManager, _userDataFactory);
            if (_rooms.TryAdd(roomId, myInstance))
            {
                room = myInstance;
                return true;
            }
        }

        room = null!;
        return false;
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
