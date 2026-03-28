using Dapper;
using Plus.Communication.Packets;
using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.Database;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.Core;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms.Chat;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Items.Data.Moodlight;
using Plus.HabboHotel.Items.Data.Toner;
using Plus.HabboHotel.Rooms.AI.Speech;
using Plus.HabboHotel.Rooms.Games;
using Plus.HabboHotel.Rooms.Games.Banzai;
using Plus.HabboHotel.Rooms.Games.Football;
using Plus.HabboHotel.Rooms.Games.Freeze;
using Plus.HabboHotel.Rooms.Games.Teams;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;
using Plus.Utilities;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.Users.UserData;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Bots;
using Plus.Core.Language;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Plus.HabboHotel.Rooms;

public class Room : RoomData
{
    private sealed class BotRow
    {
        public int Id { get; init; }
        public uint RoomId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Motto { get; init; } = string.Empty;
        public string Look { get; init; } = string.Empty;
        public int X { get; init; }
        public int Y { get; init; }
        public int Z { get; init; }
        public int Rotation { get; init; }
        public string Gender { get; init; } = string.Empty;
        public int UserId { get; init; }
        public string AiType { get; init; } = string.Empty;
        public string WalkMode { get; init; } = string.Empty;
        public bool AutomaticChat { get; init; }
        public int SpeakingInterval { get; init; }
        public string MixSentences { get; init; } = "0";
        public int ChatBubble { get; init; }
    }

    private sealed class BotSpeechRow
    {
        public string Text { get; init; } = string.Empty;
    }

    private sealed class PetBotRow
    {
        public int Id { get; init; }
        public int UserId { get; init; }
        public uint RoomId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int X { get; init; }
        public int Y { get; init; }
        public double Z { get; init; }
    }

    private sealed class PetDataRow
    {
        public int Type { get; init; }
        public string Race { get; init; } = string.Empty;
        public string Color { get; init; } = string.Empty;
        public int Experience { get; init; }
        public int Energy { get; init; }
        public int Nutrition { get; init; }
        public int Respect { get; init; }
        public double CreateStamp { get; init; }
        public int HaveSaddle { get; init; }
        public int AnyoneRide { get; init; }
        public int HairDye { get; init; }
        public int PetHair { get; init; }
        public string GnomeClothing { get; init; } = string.Empty;
    }

    private readonly BansComponent _bansComponent;

    private readonly FilterComponent _filterComponent;

    private readonly Dictionary<uint, List<RoomUser>> _tents;
    private readonly TradingComponent _tradingComponent;
    private readonly WiredComponent _wiredComponent;
    private BattleBanzai? _banzai;
    private Freeze? _freeze;
    private GameItemHandler? _gameItemHandler;
    private GameManager? _gameManager;

    private Gamemap? _gamemap;
    private RoomItemHandling? _roomItemHandling;

    private RoomUserManager? _roomUserManager;
    private Soccer? _soccer;

    public bool IsCrashed;
    public DateTime LastRegeneration;
    public DateTime LastTimerReset;
    private readonly IGameClientManager _clientManager;
    private readonly IDatabase _database;
    private readonly IGroupManager _groupManager;
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
    private readonly IRoomService _roomService;
    private readonly IChatManager _chatManager;
    private readonly IBotManager _botManager;
    private readonly IQuestService _questService;
    private readonly ICacheManager _cacheManager;
    private readonly IItemTeleporterFinder _itemTeleporterFinder;
    private readonly IItemHopperFinder _itemHopperFinder;
    private readonly IBadgeManager _badgeManager;
    private readonly IUserDataFactory _userDataFactory;
    public bool MDisposed;
    public MoodlightData? MoodlightData;

    public Dictionary<int, double> MutedUsers;

    public Task? ProcessTask;
    public bool RoomMuted;

    public TeamManager? Teambanzai;
    public TeamManager? Teamfreeze;

    public TonerData? TonerData;

    public List<int> UsersWithRights = new();
    public RoomData Data => this;
    private readonly IAchievementService _achievementService;
    private readonly IRoomManager _roomManager;
    private readonly ILanguageManager _languageManager;

    public Room(RoomData data, IGameClientManager clientManager, IDatabase database, IItemLoader itemLoader, IRoomItemPersistenceService roomItemPersistenceService, IRoomItemPlacementValidatorService roomItemPlacementValidatorService, IRoomItemPlacementPersistenceService roomItemPlacementPersistenceService, IRoomRollerService roomRollerService, IRoomItemInventoryService roomItemInventoryService, IRoomItemUpdateQueueService roomItemUpdateQueueService, IRoomItemLoadService roomItemLoadService, IRoomItemRemovalService roomItemRemovalService, IRoomItemStateService roomItemStateService, IGroupManager groupManager, IRoomService roomService, IChatManager chatManager, IBotManager botManager, IAchievementService achievementService, IQuestService questService, ICacheManager cacheManager, ILanguageManager languageManager, IItemTeleporterFinder itemTeleporterFinder, IItemHopperFinder itemHopperFinder, IBadgeManager badgeManager, IUserDataFactory userDataFactory, IRoomManager roomManager, ILoggerFactory loggerFactory)
        : base(data)
    {
        _clientManager = clientManager;
        _database = database;
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
        _achievementService = achievementService;
        _roomManager = roomManager;
        IsLagging = 0;
        Unloaded = false;
        IdleTime = 0;
        RoomMuted = false;
        MutedUsers = new();
        _tents = new();
        _gamemap = new(this, data.Model);
        _roomItemHandling = new(this, _itemLoader, _roomItemPersistenceService, _roomItemPlacementValidatorService, _roomItemPlacementPersistenceService, _roomRollerService, _roomItemInventoryService, _roomItemUpdateQueueService, _roomItemLoadService, _roomItemRemovalService, _roomItemStateService);
        _roomUserManager = new(this, clientManager, database, groupManager);
        _filterComponent = new(this);
        _wiredComponent = new(this, loggerFactory.CreateLogger<WiredComponent>());
        _bansComponent = new(this);
        _tradingComponent = new(this);
        InitializeRoomContent(database);
        LastRegeneration = DateTime.Now;
    }

    private void InitializeRoomContent(IDatabase database)
    {
        GetRoomItemHandler().LoadFurniture();
        GetGameMap().GenerateMaps();
        InitializeRoomStateContent(database);
        InitializeRoomCreatures();
    }

    private void InitializeRoomStateContent(IDatabase database)
    {
        LoadPromotions(database);
        LoadRights();
        LoadFilter();
    }

    private void InitializeRoomCreatures()
    {
        InitBots();
        InitPets();
    }

    public void OnCycle() => ProcessRoom();

    public IRoomService GetRoomService() => _roomService;
    public IChatManager GetChatManager() => _chatManager;
    public IBotManager GetBotManager() => _botManager;
    public IGameClientManager GetClientManager() => _clientManager;
    public IDatabase GetDatabase() => _database;
    public IAchievementService GetAchievementService() => _achievementService;
    public IQuestService GetQuestService() => _questService;
    public ICacheManager GetCacheManager() => _cacheManager;
    public IBadgeManager GetBadgeManager() => _badgeManager;
    public IUserDataFactory GetUserDataFactory() => _userDataFactory;
    public IGroupManager GetGroupManager() => _groupManager;
    public IItemTeleporterFinder GetItemTeleporterFinder() => _itemTeleporterFinder;
    public IItemHopperFinder GetItemHopperFinder() => _itemHopperFinder;

    public int IsLagging { get; set; }
    public bool Unloaded { get; set; }
    public int IdleTime { get; set; }

    public List<string> WordFilterList { get; set; } = new();

    public int UserCount => GetRoomUserManager().GetRoomUsers().Count;

    public uint RoomId => Id;

    public bool CanTradeInRoom => true;

    public Gamemap GetGameMap() => _gamemap!;

    public RoomItemHandling GetRoomItemHandler()
    {
        if (_roomItemHandling == null) _roomItemHandling = new(this, _itemLoader, _roomItemPersistenceService, _roomItemPlacementValidatorService, _roomItemPlacementPersistenceService, _roomRollerService, _roomItemInventoryService, _roomItemUpdateQueueService, _roomItemLoadService, _roomItemRemovalService, _roomItemStateService);
        return _roomItemHandling;
    }

    public RoomUserManager GetRoomUserManager() => _roomUserManager!;

    public Soccer GetSoccer()
    {
        if (_soccer == null)
            _soccer = new(this);
        return _soccer;
    }

    public TeamManager GetTeamManagerForBanzai()
    {
        if (Teambanzai == null)
            Teambanzai = TeamManager.CreateTeam("banzai");
        return Teambanzai;
    }

    public TeamManager GetTeamManagerForFreeze()
    {
        if (Teamfreeze == null)
            Teamfreeze = TeamManager.CreateTeam("freeze");
        return Teamfreeze;
    }

    public BattleBanzai GetBanzai()
    {
        if (_banzai == null)
            _banzai = new(this);
        return _banzai;
    }

    public Freeze GetFreeze()
    {
        if (_freeze == null)
            _freeze = new(this);
        return _freeze;
    }

    public GameManager GetGameManager()
    {
        if (_gameManager == null)
            _gameManager = new(this);
        return _gameManager;
    }

    public GameItemHandler GetGameItemHandler()
    {
        if (_gameItemHandler == null)
            _gameItemHandler = new(this);
        return _gameItemHandler;
    }

    public bool GotSoccer() => _soccer != null;

    public bool GotBanzai() => _banzai != null;

    public bool GotFreeze() => _freeze != null;

    public void ClearTags()
    {
        Tags.Clear();
    }

    public void AddTagRange(List<string> tags)
    {
        Tags.AddRange(tags);
    }

    public void InitBots()
    {
        var roomUserManager = _roomUserManager;
        if (roomUserManager == null)
            return;

        using var connection = _database.Connection();
        var bots = connection.Query<BotRow>(
            """
            SELECT
                `id` AS Id,
                `room_id` AS RoomId,
                `name` AS Name,
                `motto` AS Motto,
                `look` AS Look,
                `x` AS X,
                `y` AS Y,
                `z` AS Z,
                `rotation` AS Rotation,
                `gender` AS Gender,
                `user_id` AS UserId,
                `ai_type` AS AiType,
                `walk_mode` AS WalkMode,
                `automatic_chat` AS AutomaticChat,
                `speaking_interval` AS SpeakingInterval,
                `mix_sentences` AS MixSentences,
                `chat_bubble` AS ChatBubble
            FROM `bots`
            WHERE `room_id` = @roomId AND `ai_type` != 'pet'
            """,
            new { roomId = RoomId });
        foreach (var bot in bots)
        {
            var speeches = LoadBotSpeeches(connection, bot.Id);
            var botData = CreateBotData(bot, ref speeches);
            roomUserManager.DeployBot(botData, null!);
        }
    }

    public void InitPets()
    {
        var roomUserManager = _roomUserManager;
        if (roomUserManager == null)
            return;

        using var connection = _database.Connection();
        var pets = connection.Query<PetBotRow>(
            "SELECT `id` AS Id, `user_id` AS UserId, `room_id` AS RoomId, `name` AS Name, `x` AS X, `y` AS Y, `z` AS Z FROM `bots` WHERE `room_id` = @roomId AND `ai_type` = 'pet'",
            new { roomId = RoomId });
        foreach (var row in pets)
        {
            var mRow = connection.QueryFirstOrDefault<PetDataRow>(
                """
                SELECT
                    `type` AS Type,
                    `race` AS Race,
                    `color` AS Color,
                    `experience` AS Experience,
                    `energy` AS Energy,
                    `nutrition` AS Nutrition,
                    `respect` AS Respect,
                    `createstamp` AS CreateStamp,
                    `have_saddle` AS HaveSaddle,
                    `anyone_ride` AS AnyoneRide,
                    `hairdye` AS HairDye,
                    `pethair` AS PetHair,
                    `gnome_clothing` AS GnomeClothing
                FROM `bots_petdata`
                WHERE `id` = @id
                LIMIT 1
                """,
                new { id = row.Id });
            if (mRow == null)
                continue;
            var pet = CreatePet(row, mRow);
            var petData = CreatePetBotData(pet);
            roomUserManager.DeployBot(petData, pet);
        }
    }

    private List<RandomSpeech> LoadBotSpeeches(IDbConnection connection, int botId)
    {
        var speeches = new List<RandomSpeech>();
        foreach (var speech in connection.Query<BotSpeechRow>(
                     "SELECT `text` AS Text FROM `bots_speech` WHERE `bot_id` = @botId",
                     new { botId }))
            speeches.Add(new(speech.Text, botId));

        return speeches;
    }

    private RoomBot CreateBotData(BotRow bot, ref List<RandomSpeech> speeches)
    {
        return new(bot.Id, bot.RoomId, bot.AiType, bot.WalkMode, bot.Name,
            bot.Motto, bot.Look, bot.X, bot.Y, bot.Z,
            bot.Rotation, 0, 0, 0, 0, ref speeches, "M", 0, bot.UserId, bot.AutomaticChat,
            bot.SpeakingInterval, ConvertExtensions.EnumToBool(bot.MixSentences), bot.ChatBubble);
    }

    private Pet CreatePet(PetBotRow row, PetDataRow petData)
    {
        var pet = new Pet(row.Id, row.UserId, row.RoomId, row.Name, petData.Type,
            petData.Race,
            petData.Color, petData.Experience, petData.Energy, petData.Nutrition, petData.Respect,
            petData.CreateStamp, row.X, row.Y,
            row.Z, petData.HaveSaddle, petData.AnyoneRide, petData.HairDye, petData.PetHair,
            petData.GnomeClothing);
        pet.Room = this;
        pet.OwnerName = _clientManager.GetNameById(pet.OwnerId).Result;
        return pet;
    }

    private RoomBot CreatePetBotData(Pet pet)
    {
        var randomSpeeches = new List<RandomSpeech>();
        return new(pet.PetId, RoomId, "pet", "freeroam", pet.Name, "", pet.Look, pet.X, pet.Y, Convert.ToInt32(pet.Z), 0, 0, 0, 0, 0, ref randomSpeeches, "", 0, pet.OwnerId, false, 0, false,
            0);
    }

    public FilterComponent GetFilter() => _filterComponent;

    public WiredComponent GetWired() => _wiredComponent;

    public BansComponent GetBans() => _bansComponent;

    public TradingComponent GetTrading() => _tradingComponent;

    public void LoadRights()
    {
        if (Group != null)
            return;

        using var connection = _database.Connection();
        UsersWithRights = LoadRoomRights(connection);
    }

    private void LoadFilter()
    {
        using var connection = _database.Connection();
        WordFilterList = LoadRoomFilterWords(connection);
    }

    private List<int> LoadRoomRights(IDbConnection connection)
    {
        return connection.Query<int>(
            "SELECT `user_id` FROM `room_rights` WHERE `room_id` = @roomId",
            new { roomId = Id }).ToList();
    }

    private List<string> LoadRoomFilterWords(IDbConnection connection)
    {
        return connection.Query<string>(
            "SELECT `word` FROM `room_filter` WHERE `room_id` = @roomId",
            new { roomId = Id }).Select(word => word ?? string.Empty).ToList();
    }

    public bool CheckRights(GameClient session) => CheckRights(session, false);

    public bool CheckRights(GameClient session, bool requireOwnership, bool checkForGroups = false)
    {
        try
        {
            var habbo = session?.GetHabbo();
            if (habbo == null)
                return false;

            var group = Group;
            if (habbo.Username == OwnerName && Type == "private")
                return true;
            if (habbo.Permissions?.HasRight("room_any_owner") == true)
                return true;
            if (!requireOwnership && Type == "private")
            {
                if (habbo.Permissions?.HasRight("room_any_rights") == true)
                    return true;
                if (UsersWithRights.Contains(habbo.Id))
                    return true;
            }
            if (checkForGroups && Type == "private")
            {
                if (group == null)
                    return false;
                if (group.IsAdmin(habbo.Id))
                    return true;
                if (group.AdminOnlyDeco == 0)
                {
                    if (group.IsAdmin(habbo.Id))
                        return true;
                }
            }
        }
        catch (Exception e)
        {
            ExceptionLogger.LogException(e);
        }
        return false;
    }

    public void OnUserShoot(RoomUser user, Item ball)
    {
        Func<Item, bool>? predicate = null;
        string? key = null;
        foreach (var item in GetRoomItemHandler().GetFurniObjects(ball.GetX, ball.GetY).ToList())
        {
            if (item.Definition.ItemName.StartsWith("fball_goal_"))
            {
                key = item.Definition.ItemName.Split(new[] { '_' })[2];
                user.UnIdle();
                user.DanceId = 0;
                _ = _achievementService.ProgressAchievement(user.GetClient(), "ACH_FootballGoalScored", 1);
                SendPacket(new ActionComposer(user.VirtualId, 1));
            }
        }
        if (key != null)
        {
            if (predicate == null) predicate = p => p.Definition.ItemName == $"fball_score_{key}";
            foreach (var item2 in GetRoomItemHandler().GetFloor.Where(predicate).ToList())
            {
                if (item2.Definition.ItemName == $"fball_score_{key}")
                {
                    if (!string.IsNullOrEmpty(item2.LegacyDataString))
                        item2.LegacyDataString = (Convert.ToInt32(item2.LegacyDataString) + 1).ToString();
                    else
                        item2.LegacyDataString = "1";
                    item2.UpdateState();
                }
            }
        }
    }

    public void ProcessRoom()
    {
        if (IsCrashed || MDisposed)
            return;

        try
        {
            var roomUserManager = GetRoomUserManager();
            ExecuteRoomPhase(GetRoomItemHandler().OnCycle);
            ExecuteRoomPhase(roomUserManager.OnCycle);
            ExecuteRoomPhase(roomUserManager.SerializeStatusUpdates);
            ExecuteRoomPhase(RunGameItemCycle);
            ExecuteRoomPhase(GetWired().OnCycle);
        }
        catch (Exception e)
        {
            ExceptionLogger.LogException(e);
            OnRoomCrash(e);
        }
    }

    private void ExecuteRoomPhase(Action phase)
    {
        try
        {
            phase();
        }
        catch (Exception e)
        {
            ExceptionLogger.LogException(e);
        }
    }

    private void RunGameItemCycle()
    {
        if (_gameItemHandler != null)
            _gameItemHandler.OnCycle();
    }

    public void UpdateLifecycleState()
    {
        if (HasActivePromotion && Promotion?.HasExpired == true)
            EndPromotion();

        if (HasUsers())
        {
            if (IdleTime > 0)
                IdleTime = 0;
            return;
        }

        IdleTime++;
    }

    public bool HasUsers() => GetRoomUserManager().UserCount > 0;

    public bool ShouldUnloadForInactivity() => IdleTime >= 60 && !HasActivePromotion;

    private void OnRoomCrash(Exception e)
    {
        try
        {
            NotifyAndEvictUsersAfterCrash();
        }
        catch (Exception crashHandlingException)
        {
            ExceptionLogger.LogException(crashHandlingException);
        }

        IsCrashed = true;
        _roomManager.UnloadRoom(Id);
    }

    private void NotifyAndEvictUsersAfterCrash()
    {
        var roomUserManager = _roomUserManager;
        if (roomUserManager == null)
            return;

        foreach (var user in roomUserManager.GetRoomUsers().ToList())
            NotifyAndEvictUserAfterCrash(user);
    }

    private void NotifyAndEvictUserAfterCrash(RoomUser? user)
    {
        var client = user?.GetClient();
        if (client == null)
            return;

        client.SendNotification("Sorry, it appears that room has crashed!");
        try
        {
            GetRoomUserManager().RemoveUserFromRoom(client, true);
        }
        catch (Exception removalException)
        {
            ExceptionLogger.LogException(removalException);
        }
    }


    public bool CheckMute(GameClient session)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return false;
        var habboId = habbo.Id;

        if (MutedUsers.ContainsKey(habboId))
        {
            if (MutedUsers[habboId] < UnixTimestamp.GetNow())
                MutedUsers.Remove(habboId);
            else
                return true;
        }
        if (habbo.TimeMuted > 0 || RoomMuted && habbo.Username != OwnerName)
            return true;
        return false;
    }

    public void SendObjects(GameClient session)
    {
        var roomUserManager = _roomUserManager;
        if (roomUserManager == null)
            return;
        var users = roomUserManager.GetUserList().ToList();

        session.Send(new HeightMapComposer(GetGameMap().Model.Heightmap));
        session.Send(new FloorHeightMapComposer(GetGameMap().Model.GetRelativeHeightmap(), GetGameMap().StaticModel.WallHeight));
        foreach (var user in users)
        {
            if (user == null)
                continue;
            session.Send(new UsersComposer(user, _groupManager));
            if (user.IsBot && user.BotData.DanceId > 0)
                session.Send(new DanceComposer(user, user.BotData.DanceId));
            else if (!user.IsBot && !user.IsPet && user.IsDancing)
                session.Send(new DanceComposer(user, user.DanceId));
            if (user.IsAsleep)
                session.Send(new SleepComposer(user, true));
            if (user.CarryItemId > 0 && user.CarryTimer > 0)
                session.Send(new CarryObjectComposer(user.VirtualId, user.CarryItemId));
            if (!user.IsBot && !user.IsPet && user.CurrentEffect > 0)
                session.Send(new AvatarEffectComposer(user.VirtualId, user.CurrentEffect));
        }
        session.Send(new UserUpdateComposer(users));
        session.Send(new ObjectsComposer(GetRoomItemHandler().GetFloor.ToArray(), this));
        session.Send(new ItemsComposer(GetRoomItemHandler().GetWall.ToArray(), this));
    }

    public void AddTent(uint tentId)
    {
        if (_tents.ContainsKey(tentId))
            _tents.Remove(tentId);
        _tents.Add(tentId, new());
    }

    public void RemoveTent(uint tentId)
    {
        if (!_tents.ContainsKey(tentId))
            return;
        var users = _tents[tentId];
        foreach (var user in users.ToList())
        {
            var habbo = GetHabbo(user);
            if (habbo == null)
                continue;
            habbo.TentId = 0;
        }
        if (_tents.ContainsKey(tentId))
            _tents.Remove(tentId);
    }

    public void AddUserToTent(uint tentId, RoomUser user)
    {
        var habbo = GetHabbo(user);
        if (habbo == null)
            return;
        if (!_tents.ContainsKey(tentId))
            _tents.Add(tentId, new());
        if (!_tents[tentId].Contains(user))
            _tents[tentId].Add(user);
        habbo.TentId = tentId;
    }

    public void RemoveUserFromTent(uint tentId, RoomUser user)
    {
        var habbo = GetHabbo(user);
        if (habbo == null)
            return;
        if (!_tents.ContainsKey(tentId))
            _tents.Add(tentId, new());
        if (_tents[tentId].Contains(user))
            _tents[tentId].Remove(user);
        habbo.TentId = 0;
    }

    public void SendToTent(int id, uint tentId, IServerPacket packet)
    {
        if (!_tents.ContainsKey(tentId))
            return;
        foreach (var user in _tents[tentId].ToList())
        {
            var client = user?.GetClient();
            var habbo = GetHabbo(user);
            if (client == null || habbo == null || habbo.IgnoresComponent?.IsIgnored(id) == true || habbo.TentId != tentId)
                continue;
            client.Send(packet);
        }
    }

    private Habbo? GetHabbo(RoomUser? user)
    {
        var client = user?.GetClient();
        return client?.GetHabbo();
    }

    public void SendPacket(IServerPacket packet, bool withRightsOnly = false)
    {
        if (packet == null)
            return;
        try
        {
            var roomUserManager = _roomUserManager;
            if (roomUserManager == null)
                return;

            var users = roomUserManager.GetUserList().ToList();
            foreach (var user in users)
            {
                if (user == null)
                    continue;
                var client = user.GetClient();
                if (client == null || user.IsBot)
                    continue;
                if (withRightsOnly && !CheckRights(client))
                    continue;
                client.Send(packet);
            }
        }
        catch (Exception e)
        {
            ExceptionLogger.LogException(e);
        }
    }

    public void SendPacket(List<IServerPacket> packets)
    {
        foreach (var packet in packets)
            SendPacket(packet);
    }

    public void Dispose()
    {
        SendPacket(new CloseConnectionComposer());
        if (MDisposed)
            return;

        IsCrashed = false;
        MDisposed = true;
        DisposeProcessTask();
        ResetRoomCollections();
        DisposeRoomSystems();
        CleanupRoomComponents();
    }

    private void DisposeProcessTask()
    {
        try
        {
            if (ProcessTask != null && ProcessTask.IsCompleted)
                ProcessTask.Dispose();
        }
        catch
        {
        }
    }

    private void ResetRoomCollections()
    {
        TonerData = null;
        MoodlightData = null;
        if (MutedUsers.Count > 0)
            MutedUsers.Clear();
        if (_tents.Count > 0)
            _tents.Clear();
        if (UsersWithRights.Count > 0)
            UsersWithRights.Clear();
        if (WordFilterList.Count > 0)
            WordFilterList.Clear();
    }

    private void DisposeRoomSystems()
    {
        DisposeGameSystems();
        DisposeTeamManagers();
        DisposeRoomManagers();
    }

    private void DisposeGameSystems()
    {
        DisposeAndClear(ref _gameManager, static gameManager => gameManager.Dispose());
        DisposeAndClear(ref _freeze, static freeze => freeze.Dispose());
        DisposeAndClear(ref _soccer, static soccer => soccer.Dispose());
        DisposeAndClear(ref _banzai, static banzai => banzai.Dispose());
        DisposeAndClear(ref _gamemap, static gamemap => gamemap.Dispose());
        DisposeAndClear(ref _gameItemHandler, static gameItemHandler => gameItemHandler.Dispose());
    }

    private void DisposeTeamManagers()
    {
        DisposeAndClear(ref Teambanzai, static teamManager => teamManager.Dispose());
        DisposeAndClear(ref Teamfreeze, static teamManager => teamManager.Dispose());
    }

    private void DisposeRoomManagers()
    {
        DisposeAndClear(ref _roomUserManager, static roomUserManager => roomUserManager.Dispose());
        DisposeAndClear(ref _roomItemHandling, static roomItemHandling => roomItemHandling.Dispose());
    }

    private void CleanupRoomComponents()
    {
        _filterComponent?.Cleanup();
        _wiredComponent?.Cleanup();
        _bansComponent?.Cleanup();
        _tradingComponent?.Cleanup();
    }

    private static void DisposeAndClear<T>(ref T? value, Action<T> disposeAction) where T : class
    {
        if (value != null)
            disposeAction(value);
        value = null;
    }
}
