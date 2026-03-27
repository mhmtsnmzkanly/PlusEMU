using Dapper;
using Plus.Communication.Packets;
using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.Database;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.Core;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Items.Data.Moodlight;
using Plus.HabboHotel.Items.Data.Toner;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.AI.Speech;
using Plus.HabboHotel.Rooms.Games;
using Plus.HabboHotel.Rooms.Games.Banzai;
using Plus.HabboHotel.Rooms.Games.Football;
using Plus.HabboHotel.Rooms.Games.Freeze;
using Plus.HabboHotel.Rooms.Games.Teams;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;
using Plus.Utilities;

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
    public bool MDisposed;
    public MoodlightData? MoodlightData;

    public Dictionary<int, double> MutedUsers;

    public Task? ProcessTask;
    public bool RoomMuted;

    public TeamManager? Teambanzai;
    public TeamManager? Teamfreeze;

    public TonerData? TonerData;

    public List<int> UsersWithRights = new();

    public Room(RoomData data, IGameClientManager clientManager, IDatabase database, IItemLoader itemLoader, IGroupManager groupManager)
        : base(data)
    {
        _clientManager = clientManager;
        _database = database;
        _itemLoader = itemLoader;
        _groupManager = groupManager;
        IsLagging = 0;
        Unloaded = false;
        IdleTime = 0;
        RoomMuted = false;
        MutedUsers = new();
        _tents = new();
        _gamemap = new(this, data.Model);
        _roomItemHandling = new(this, _itemLoader);
        _roomUserManager = new(this, clientManager, database, groupManager);
        _filterComponent = new(this);
        _wiredComponent = new(this);
        _bansComponent = new(this);
        _tradingComponent = new(this);
        GetRoomItemHandler().LoadFurniture();
        GetGameMap().GenerateMaps();
        LoadPromotions();
        LoadRights();
        LoadFilter();
        InitBots();
        InitPets();
        LastRegeneration = DateTime.Now;
    }

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
        if (_roomItemHandling == null) _roomItemHandling = new(this, _itemLoader);
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

        using var connection = PlusEnvironment.DatabaseManager.Connection();
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
            var speeches = new List<RandomSpeech>();
            foreach (var speech in connection.Query<BotSpeechRow>(
                         "SELECT `text` AS Text FROM `bots_speech` WHERE `bot_id` = @botId",
                         new { botId = bot.Id }))
                speeches.Add(new(speech.Text, bot.Id));
            roomUserManager.DeployBot(
                new(bot.Id, bot.RoomId, bot.AiType, bot.WalkMode, bot.Name,
                    bot.Motto, bot.Look, bot.X, bot.Y, bot.Z,
                    bot.Rotation, 0, 0, 0, 0, ref speeches, "M", 0, bot.UserId, bot.AutomaticChat,
                    bot.SpeakingInterval, ConvertExtensions.EnumToBool(bot.MixSentences), bot.ChatBubble), null!);
        }
    }

    public void InitPets()
    {
        var roomUserManager = _roomUserManager;
        if (roomUserManager == null)
            return;

        using var connection = PlusEnvironment.DatabaseManager.Connection();
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
            var pet = new Pet(row.Id, row.UserId, row.RoomId, row.Name, mRow.Type,
                mRow.Race,
                mRow.Color, mRow.Experience, mRow.Energy, mRow.Nutrition, mRow.Respect,
                mRow.CreateStamp, row.X, row.Y,
                row.Z, mRow.HaveSaddle, mRow.AnyoneRide, mRow.HairDye, mRow.PetHair,
                mRow.GnomeClothing);
            var rndSpeechList = new List<RandomSpeech>();
            roomUserManager.DeployBot(
                new(pet.PetId, RoomId, "pet", "freeroam", pet.Name, "", pet.Look, pet.X, pet.Y, Convert.ToInt32(pet.Z), 0, 0, 0, 0, 0, ref rndSpeechList, "", 0, pet.OwnerId, false, 0, false,
                    0), pet);
        }
    }

    public FilterComponent GetFilter() => _filterComponent;

    public WiredComponent GetWired() => _wiredComponent;

    public BansComponent GetBans() => _bansComponent;

    public TradingComponent GetTrading() => _tradingComponent;

    public void LoadRights()
    {
        UsersWithRights = new();
        if (Group != null)
            return;
        using (var connection = PlusEnvironment.DatabaseManager.Connection())
        {
            foreach (var userId in connection.Query<int>(
                         "SELECT `user_id` FROM `room_rights` WHERE `room_id` = @roomId",
                         new { roomId = Id }))
                UsersWithRights.Add(userId);
        }
    }

    private void LoadFilter()
    {
        WordFilterList = new();
        using (var connection = PlusEnvironment.DatabaseManager.Connection())
        {
            foreach (var word in connection.Query<string>(
                         "SELECT `word` FROM `room_filter` WHERE `room_id` = @roomId",
                         new { roomId = Id }))
                WordFilterList.Add(word ?? string.Empty);
        }
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
                _ = PlusEnvironment.Game.AchievementService.ProgressAchievement(user.GetClient(), "ACH_FootballGoalScored", 1);
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
            if (roomUserManager.GetRoomUsers().Count == 0)
                IdleTime++;
            else if (IdleTime > 0)
                IdleTime = 0;
            if (HasActivePromotion && Promotion?.HasExpired == true) EndPromotion();
            if (IdleTime >= 60 && !HasActivePromotion)
            {
                PlusEnvironment.Game.RoomManager.UnloadRoom(Id);
                return;
            }
            try
            {
                GetRoomItemHandler().OnCycle();
            }
            catch (Exception e)
            {
                ExceptionLogger.LogException(e);
            }
            try
            {
                roomUserManager.OnCycle();
            }
            catch (Exception e)
            {
                ExceptionLogger.LogException(e);
            }
            try
            {
                GetRoomUserManager().SerializeStatusUpdates();
            }
            catch (Exception e)
            {
                ExceptionLogger.LogException(e);
            }
            try
            {
                if (_gameItemHandler != null)
                    _gameItemHandler.OnCycle();
            }
            catch (Exception e)
            {
                ExceptionLogger.LogException(e);
            }
            try
            {
                GetWired().OnCycle();
            }
            catch (Exception e)
            {
                ExceptionLogger.LogException(e);
            }
        }
        catch (Exception e)
        {
            ExceptionLogger.LogException(e);
            OnRoomCrash(e);
        }
    }

    private void OnRoomCrash(Exception e)
    {
        try
        {
            var roomUserManager = _roomUserManager;
            if (roomUserManager == null)
                return;

            foreach (var user in roomUserManager.GetRoomUsers().ToList())
            {
                if (user == null || user.GetClient() == null)
                    continue;
                user.GetClient().SendNotification("Sorry, it appears that room has crashed!"); //Unhandled exception in room: " + e);
                try
                {
                    GetRoomUserManager().RemoveUserFromRoom(user.GetClient(), true);
                }
                catch (Exception e2)
                {
                    ExceptionLogger.LogException(e2);
                }
            }
        }
        catch (Exception e3)
        {
            ExceptionLogger.LogException(e3);
        }
        IsCrashed = true;
        PlusEnvironment.Game.RoomManager.UnloadRoom(Id);
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
        if (!MDisposed)
        {
            IsCrashed = false;
            MDisposed = true;
            /* TODO: Needs reviewing */
            try
            {
                if (ProcessTask != null && ProcessTask.IsCompleted)
                    ProcessTask.Dispose();
            }
            catch { }
            TonerData = null;
            MoodlightData = null;
            if (MutedUsers.Count > 0)
                MutedUsers.Clear();
            if (_tents.Count > 0)
                _tents.Clear();
            if (UsersWithRights.Count > 0)
                UsersWithRights.Clear();
            if (_gameManager != null)
            {
                _gameManager.Dispose();
                _gameManager = null;
            }
            if (_freeze != null)
            {
                _freeze.Dispose();
                _freeze = null;
            }
            if (_soccer != null)
            {
                _soccer.Dispose();
                _soccer = null;
            }
            if (_banzai != null)
            {
                _banzai.Dispose();
                _banzai = null;
            }
            if (_gamemap != null)
            {
                _gamemap.Dispose();
                _gamemap = null;
            }
            if (_gameItemHandler != null)
            {
                _gameItemHandler.Dispose();
                _gameItemHandler = null;
            }

            // Room Data?
            if (Teambanzai != null)
            {
                Teambanzai.Dispose();
                Teambanzai = null;
            }
            if (Teamfreeze != null)
            {
                Teamfreeze.Dispose();
                Teamfreeze = null;
            }
            if (_roomUserManager != null)
            {
                _roomUserManager.Dispose();
                _roomUserManager = null;
            }
            if (_roomItemHandling != null)
            {
                _roomItemHandling.Dispose();
                _roomItemHandling = null;
            }
            if (WordFilterList.Count > 0)
                WordFilterList.Clear();
            if (_filterComponent != null)
                _filterComponent.Cleanup();
            if (_wiredComponent != null)
                _wiredComponent.Cleanup();
            if (_bansComponent != null)
                _bansComponent.Cleanup();
            if (_tradingComponent != null)
                _tradingComponent.Cleanup();
        }
    }
}
