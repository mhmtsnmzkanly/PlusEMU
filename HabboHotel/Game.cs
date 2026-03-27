using Plus.Core;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.Bots;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Games;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Televisions;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Navigator;
using Plus.HabboHotel.Permissions;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rewards;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat;
using Plus.HabboHotel.Catalog.Utilities;
using Plus.HabboHotel.Subscriptions;
using Plus.HabboHotel.Talents;

namespace Plus.HabboHotel;

public class Game : IGame
{
    private readonly IGameClientManager _clientManager;
    private readonly IModerationManager _moderationManager;
    private readonly IItemDataManager _itemDataManager;
    private readonly ICatalogManager _catalogManager;
    private readonly ITelevisionManager _televisionManager;
    private readonly INavigatorManager _navigatorManager;
    private readonly IRoomManager _roomManager;
    private readonly IChatManager _chatManager;
    private readonly IGroupManager _groupManager;
    private readonly IQuestManager _questManager;
    private readonly IQuestService _questService;
    private readonly ICatalogService _catalogService;
    private readonly IAchievementService _achievementService;
    private readonly IAchievementManager _achievementManager;
    private readonly IRoomService _roomService;
    private readonly IRoomFactory _roomFactory;
    private readonly IRoomAppender _roomAppender;
    private readonly IItemService _itemService;
    private readonly IBotUtility _botUtility;
    private readonly IPetUtility _petUtility;

    private IBadgeManager _badgeManager;
    private IBotManager _botManager;
    private ICacheManager _cacheManager;
    private readonly int _cycleSleepTime = 25;
    private IGameDataManager _gameDataManager;
    private IServerStatusUpdater _globalUpdater;
    private IPermissionManager _permissionManager;
    private IRewardManager _rewardManager;
    private ISubscriptionManager _subscriptionManager;
    private ITalentTrackManager _talentTrackManager;
    private bool _cycleActive;

    private bool _cycleEnded;
    private Task? _gameCycle;

    public Game(
        IGameClientManager gameClientManager,
        IModerationManager moderationManager,
        IItemDataManager itemDataManager,
        ICatalogManager catalogManager,
        ITelevisionManager televisionManager,
        INavigatorManager navigatorManager,
        IRoomManager roomManager,
        IChatManager chatManager,
        IGroupManager groupManager,
        IQuestManager questManager,
        IQuestService questService,
        ICatalogService catalogService,
        IAchievementService achievementService,
        IAchievementManager achievementManager,
        ITalentTrackManager talentTrackManager,
        IGameDataManager gameDataManager,
        IServerStatusUpdater serverStatusUpdater,
        IBotManager botManager,
        ICacheManager cacheManager,
        IRewardManager rewardManager,
        IBadgeManager badgeManager,
        ISubscriptionManager subscriptionManager,
        IPermissionManager permissionManager,
        IRoomService roomService,
        IRoomFactory roomFactory,
        IRoomAppender roomAppender,
        IItemService itemService,
        IBotUtility botUtility,
        IPetUtility petUtility)
    {
        _clientManager = gameClientManager;
        _moderationManager = moderationManager;
        _itemDataManager = itemDataManager;
        _catalogManager = catalogManager;
        _televisionManager = televisionManager;
        _navigatorManager = navigatorManager;
        _roomManager = roomManager;
        _chatManager = chatManager;
        _groupManager = groupManager;
        _questManager = questManager;
        _questService = questService;
        _catalogService = catalogService;
        _achievementService = achievementService;
        _achievementManager = achievementManager;
        _talentTrackManager = talentTrackManager;
        _gameDataManager = gameDataManager;
        _globalUpdater = serverStatusUpdater;
        _botManager = botManager;
        _cacheManager = cacheManager;
        _rewardManager = rewardManager;
        _badgeManager = badgeManager;
        _subscriptionManager = subscriptionManager;
        _permissionManager = permissionManager;
        _roomService = roomService;
        _roomFactory = roomFactory;
        _roomAppender = roomAppender;
        _itemService = itemService;
        _botUtility = botUtility;
        _petUtility = petUtility;
    }

    public Task Init()
    {
        _moderationManager.Init();
        _televisionManager.Init();
        _navigatorManager.Init();
        _roomManager.LoadModels();
        _chatManager.Init();
        _groupManager.Init();
        _questManager.Init();
        _talentTrackManager.Init();
        _gameDataManager.Init();
        _globalUpdater.Init();
        _botManager.Init();
        _rewardManager.Init();
        _badgeManager.Init();
        _permissionManager.Init();
        _subscriptionManager.Init();
        _cacheManager.Init();
        return Task.CompletedTask;
    }

    public void StartGameLoop()
    {
        _gameCycle = new(GameCycle);
        _gameCycle.Start();
        _cycleActive = true;
    }

    private void GameCycle()
    {
        while (_cycleActive)
        {
            _cycleEnded = false;
            _roomManager.OnCycle();
            _clientManager.OnCycle();
            _cycleEnded = true;
            Thread.Sleep(_cycleSleepTime);
        }
    }

    public void StopGameLoop()
    {
        _cycleActive = false;
        while (!_cycleEnded) Thread.Sleep(_cycleSleepTime);
    }

    public IBadgeManager BadgeManager => _badgeManager;
    public IGameClientManager ClientManager => _clientManager;
    public ICatalogManager Catalog => _catalogManager;
    public INavigatorManager Navigator => _navigatorManager;
    public IItemDataManager ItemManager => _itemDataManager;
    public IRoomManager RoomManager => _roomManager;
    public IAchievementManager AchievementManager => _achievementManager;
    public ISubscriptionManager SubscriptionManager => _subscriptionManager;
    public IQuestManager QuestManager => _questManager;
    public IQuestService QuestService => _questService;
    public ICatalogService CatalogService => _catalogService;
    public IAchievementService AchievementService => _achievementService;
    public IGroupManager GroupManager => _groupManager;
    public IChatManager ChatManager => _chatManager;
    public IGameDataManager GameDataManager => _gameDataManager;
    public IBotManager BotManager => _botManager;
    public ICacheManager CacheManager => _cacheManager;
    public IRoomService RoomService => _roomService;
    public IRoomFactory RoomFactory => _roomFactory;
    public IRoomAppender RoomAppender => _roomAppender;
    public IItemService ItemService => _itemService;
    public IBotUtility BotUtility => _botUtility;
    public IPetUtility PetUtility => _petUtility;
}
