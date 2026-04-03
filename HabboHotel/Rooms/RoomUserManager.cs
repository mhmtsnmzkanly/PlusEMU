using System.Collections.Concurrent;
using System.Globalization;
using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.HabboHotel.Groups;
using Plus.Database;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Permissions;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.Core;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.Games.Teams;
using Plus.HabboHotel.Rooms.PathFinding;
using Plus.HabboHotel.Rooms.Trading;
using Plus.Utilities;

using Dapper;
using NLog;

namespace Plus.HabboHotel.Rooms;

public class RoomUserManager
{
    private static readonly ILogger Log = LogManager.GetLogger("Plus.HabboHotel.Rooms.RoomUserManager");
    private ConcurrentDictionary<int, RoomUser> _bots;
    private ConcurrentDictionary<int, RoomUser> _pets;

    private int _primaryPrivateUserId;
    private Room _room;
    private int _secondaryPrivateUserId;
    private ConcurrentDictionary<int, RoomUser> _users;
    private readonly IGameClientManager _clientManager;
    private readonly IDatabase _database;
    private readonly IGroupManager _groupManager;

    public int UserCount;


    public RoomUserManager(Room room, IGameClientManager clientManager, IDatabase database, IGroupManager groupManager)
    {
        _room = room;
        _clientManager = clientManager;
        _database = database;
        _groupManager = groupManager;
        _users = new();
        _pets = new();
        _bots = new();
        _primaryPrivateUserId = 1;
        _secondaryPrivateUserId = 0;
        PetCount = 0;
        UserCount = 0;
    }

    public int PetCount { get; private set; }

    public RoomUser DeployBot(RoomBot bot, Pet pet)
    {
        var virtualId = _primaryPrivateUserId++;
        var user = new RoomUser(0, _room.RoomId, virtualId, _room);
        bot.VirtualId = virtualId;
        var personalId = _secondaryPrivateUserId++;
        user.InternalRoomId = personalId;
        _users.TryAdd(personalId, user);
        var model = _room.GetGameMap().Model;
        if (bot.X > 0 && bot.Y > 0 && bot.X < model.MapSizeX && bot.Y < model.MapSizeY)
        {
            user.SetPos(bot.X, bot.Y, bot.Z);
            user.SetRot(bot.Rot, false);
        }
        else
        {
            bot.X = model.DoorX;
            bot.Y = model.DoorY;
            user.SetPos(model.DoorX, model.DoorY, model.DoorZ);
            user.SetRot(model.DoorOrientation, false);
        }
        user.BotData = bot;
        user.BotAi = bot.GenerateBotAi(user.VirtualId);
        if (user.IsPet)
        {
            user.BotAi.Init(bot.BotId, user.VirtualId, _room.RoomId, user, _room);
            user.PetData = pet;
            user.PetData.VirtualId = user.VirtualId;
        }
        else
            user.BotAi.Init(bot.BotId, user.VirtualId, _room.RoomId, user, _room);
        user.UpdateNeeded = true;
        _room.SendPacket(new UsersComposer(user, _groupManager, _room.GetCacheManager()));
        if (user.IsPet)
        {
            if (_pets.ContainsKey(user.PetData.PetId))
                _pets[user.PetData.PetId] = user;
            else
                _pets.TryAdd(user.PetData.PetId, user);
            PetCount++;
        }
        else if (user.IsBot)
        {
            if (_bots.ContainsKey(user.BotData.BotId))
                _bots[user.BotData.BotId] = user;
            else
                _bots.TryAdd(user.BotData.Id, user);
            _room.SendPacket(new DanceComposer(user, user.BotData.DanceId));
        }
        return user;
    }

    public void RemoveBot(int virtualId, bool kicked)
    {
        if (!TryGetRoomUserByVirtualId(virtualId, out var user) || user == null || !user.IsBot)
            return;
        if (user.IsPet)
        {
            _pets.TryRemove(user.PetData.PetId, out var pet);
            PetCount--;
        }
        else
            _bots.TryRemove(user.BotData.Id, out var bot);
        user.BotAi.OnSelfLeaveRoom(kicked);
        _room.SendPacket(new UserRemoveComposer(user.VirtualId));
        if (_users != null)
            _users.TryRemove(user.InternalRoomId, out var toRemove);
        OnRemove(user);
    }

    public bool TryGetUserForSquare(int x, int y, out RoomUser? user)
    {
        user = _room.GetGameMap().GetRoomUsers(new(x, y)).FirstOrDefault();
        return user != null;
    }

    internal bool AddAvatarToRoom(GameClient session)
    {
        if (_room == null)
            return false;
        if (session == null)
            return false;
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.TryGetCurrentRoom(out var currentRoom) || currentRoom != _room)
            return false;
        Log.Debug("AddAvatarToRoom start. RoomId={roomId}, SessionId={sessionId}, UserId={userId}, Username={username}", _room.RoomId, session.Id, habbo.Id, habbo.Username);
        if (_users.Any(u => u.Value.UserId == habbo.Id))
        {
            Log.Warn("AddAvatarToRoom aborted: user already exists in room. RoomId={roomId}, UserId={userId}", _room.RoomId, habbo.Id);
            return false;
        }
        var virtualId = _primaryPrivateUserId++;
        var user = new RoomUser(habbo.Id, _room.RoomId, virtualId, _room);
        user.BindClient(session);
        if (user == null || user.GetClient() == null)
        {
            Log.Warn("AddAvatarToRoom aborted: room user/client binding failed. RoomId={roomId}, UserId={userId}", _room.RoomId, habbo.Id);
            return false;
        }
        user.UserId = habbo.Id;
        habbo.TentId = 0;
        var personalId = _secondaryPrivateUserId++;
        user.InternalRoomId = personalId;
        if (!_users.TryAdd(personalId, user))
        {
            Log.Warn("AddAvatarToRoom aborted: could not add room user to registry. RoomId={roomId}, UserId={userId}, InternalRoomId={internalRoomId}", _room.RoomId, habbo.Id, personalId);
            return false;
        }
        UserCount = _users.Count(x => !x.Value.IsBot);
        _room.UsersNow = UserCount;
        var model = _room.GetGameMap().Model;
        if (model == null)
            return false;
        if (!_room.PetMorphsAllowed && habbo.PetId != 0)
            habbo.PetId = 0;
        if (!habbo.IsTeleporting && !habbo.IsHopping)
        {
            if (!model.DoorIsValid())
            {
                var square = _room.GetGameMap().GetRandomWalkableSquare();
                model.DoorX = square.X;
                model.DoorY = square.Y;
                model.DoorZ = (int)_room.GetGameMap().GetHeightForSquareFromData(square);
            }
            user.SetPos(model.DoorX, model.DoorY, model.DoorZ);
            user.SetRot(model.DoorOrientation, false);
        }
        else if (!user.IsBot && (habbo.IsTeleporting || habbo.IsHopping))
        {
            Item? item = null;
            if (habbo.IsTeleporting)
                item = _room.GetRoomItemHandler().GetItem(habbo.TeleporterId);
            else if (habbo.IsHopping)
                item = _room.GetRoomItemHandler().GetItem(habbo.HopperId);
            if (item != null)
            {
                if (habbo.IsTeleporting)
                {
                    item.LegacyDataString = "2";
                    item.UpdateState(false, true);
                    user.SetPos(item.GetX, item.GetY, item.GetZ);
                    user.SetRot(item.Rotation, false);
                    item.InteractingUser2 = habbo.Id;
                    item.LegacyDataString = "0";
                    item.UpdateState(false, true);
                }
                else if (habbo.IsHopping)
                {
                    item.LegacyDataString = "1";
                    item.UpdateState(false, true);
                    user.SetPos(item.GetX, item.GetY, item.GetZ);
                    user.SetRot(item.Rotation, false);
                    user.AllowOverride = false;
                    item.InteractingUser2 = habbo.Id;
                    item.LegacyDataString = "2";
                    item.UpdateState(false, true);
                }
            }
            else
            {
                user.SetPos(model.DoorX, model.DoorY, model.DoorZ - 1);
                user.SetRot(model.DoorOrientation, false);
            }
        }
        _room.SendPacket(new UsersComposer(user, _groupManager, _room.GetCacheManager()));
        if (_room.CheckRights(session, true))
        {
            user.SetStatus("flatctrl", "useradmin");
            session.Send(new YouAreOwnerComposer());
            session.Send(new YouAreControllerComposer(4));
        }
        else if (_room.CheckRights(session, false) && _room.Group == null)
        {
            user.SetStatus("flatctrl", "1");
            session.Send(new YouAreControllerComposer(1));
        }
        else if (_room.Group != null && _room.CheckRights(session, false, true))
        {
            user.SetStatus("flatctrl", "3");
            session.Send(new YouAreControllerComposer(3));
        }
        else
            session.Send(new YouAreNotControllerComposer());
        user.UpdateNeeded = true;
        if (habbo.Permissions != null && habbo.Effects != null)
        {
            if (habbo.Permissions.HasRight("mod_tool") && !habbo.DisableForcedEffects)
                habbo.Effects.ApplyEffect(102);
            if (habbo.IsAmbassador && !habbo.DisableForcedEffects && !habbo.Permissions.HasRight("mod_tool"))
                habbo.Effects.ApplyEffect(178);
        }
        foreach (var bot in _bots.Values.ToList())
        {
            if (bot == null || bot.BotAi == null)
                continue;
            bot.BotAi.OnUserEnterRoom(user);
        }
        Log.Info("AddAvatarToRoom completed. RoomId={roomId}, UserId={userId}, Username={username}, VirtualId={virtualId}, HumanUsers={userCount}", _room.RoomId, habbo.Id, habbo.Username, user.VirtualId, UserCount);
        return true;
    }

    internal bool RemoveUserFromRoom(GameClient session, bool nofityUser, bool notifyKick = false)
    {
        try
        {
            if (!TryGetHabboLeavingRoom(session, out var habbo))
                return false;

            Log.Info("RemoveUserFromRoom start. RoomId={roomId}, SessionId={sessionId}, UserId={userId}, Username={username}, NotifyUser={notifyUser}, NotifyKick={notifyKick}",
                _room.RoomId, session.Id, habbo.Id, habbo.Username, nofityUser, notifyKick);

            NotifyLeavingClient(session, nofityUser, notifyKick);
            ResetHabboRoomState(habbo);

            if (!TryGetRoomUserByHabbo(habbo.Id, out var user) || user == null)
            {
                Log.Warn("RemoveUserFromRoom found no room user after habbo room-state reset. RoomId={roomId}, UserId={userId}", _room.RoomId, habbo.Id);
                return true;
            }

            CleanupMountedHorse(user);
            RemoveUserFromTeam(user);
            RemoveRoomUser(user);
            ResetCurrentItemEffect(habbo, user);
            EndActiveTrade(user);
            NotifyMessengerRoomChange(habbo);
            PersistRoomExit(habbo);
            DisposeLeavingUser(user);
            Log.Info("RemoveUserFromRoom completed. RoomId={roomId}, UserId={userId}, Username={username}, RemainingHumanUsers={userCount}", _room.RoomId, habbo.Id, habbo.Username, UserCount);
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "RemoveUserFromRoom failed. RoomId={roomId}, SessionId={sessionId}", _room?.RoomId ?? 0, session?.Id);
            ExceptionLogger.LogException(e);
            return false;
        }
    }

    private bool TryGetHabboLeavingRoom(GameClient session, out Habbo habbo)
    {
        habbo = null!;
        if (_room == null || session == null)
            return false;

        habbo = session.GetHabbo();
        return habbo != null;
    }

    private static void NotifyLeavingClient(GameClient session, bool notifyUser, bool notifyKick)
    {
        if (notifyKick)
            session.Send(new GenericErrorComposer(4008));
        if (notifyUser)
            session.Send(new CloseConnectionComposer());
    }

    private static void ResetHabboRoomState(Habbo habbo)
    {
        habbo.LeaveRoom();
    }

    private void CleanupMountedHorse(RoomUser user)
    {
        if (!user.RidingHorse)
            return;

        user.RidingHorse = false;
        if (!TryGetRoomUserByVirtualId(user.HorseId, out var mountedUser) || mountedUser == null)
            return;

        mountedUser.RidingHorse = false;
        mountedUser.HorseId = 0;
    }

    private void RemoveUserFromTeam(RoomUser user)
    {
        if (user.Team == Team.None)
            return;

        var team = _room.GetTeamManagerForFreeze();
        if (team == null)
            return;

        team.OnUserLeave(user);
        user.Team = Team.None;
        var effects = user.GetClient()?.GetHabbo()?.Effects;
        if (effects != null && effects.CurrentEffect != 0)
            effects.ApplyEffect(0);
    }

    private static void ResetCurrentItemEffect(Habbo habbo, RoomUser user)
    {
        if (user.CurrentItemEffect != ItemEffectType.None && habbo.Effects != null)
            habbo.Effects.CurrentEffect = -1;
    }

    private void EndActiveTrade(RoomUser user)
    {
        if (!user.IsTrading)
            return;

        if (_room.GetTrading().TryGetTrade(user.TradeId, out Trade? trade))
            trade?.EndTrade(user.TradeId);
    }

    private static void NotifyMessengerRoomChange(Habbo habbo) => habbo.Messenger?.NotifyChangesToFriends();

    private void PersistRoomExit(Habbo habbo)
    {
        using var dbClient = _database.Connection();
        dbClient.Execute("UPDATE user_roomvisits SET exit_timestamp = @exitTimestamp WHERE room_id = @roomId AND user_id = @userId ORDER BY exit_timestamp DESC LIMIT 1",
            new
            {
                userId = habbo.Id,
                roomId = _room.RoomId,
                exitTimestamp = UnixTimestamp.GetNow(),
            });

        dbClient.Execute("UPDATE `rooms` SET `users_now` = @usersNow WHERE `id` = @roomId LIMIT 1",
            new
            {
                usersNow = _room.UsersNow,
                roomId = _room.RoomId
            });
    }

    private static void DisposeLeavingUser(RoomUser user) => user.Dispose();

    private void OnRemove(RoomUser user)
    {
        try
        {
            var session = user.GetClient();
            if (session == null)
                return;
            var bots = new List<RoomUser>();
            try
            {
                foreach (var roomUser in GetUserList().ToList())
                {
                    if (roomUser == null)
                        continue;
                    if (roomUser.IsBot && !roomUser.IsPet)
                    {
                        if (!bots.Contains(roomUser))
                            bots.Add(roomUser);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to enumerate bots during room leave handling. RoomId={roomId}, UserId={userId}", _room.RoomId, user.UserId);
            }
            var petsToRemove = new List<RoomUser>();
            foreach (var bot in bots.ToList())
            {
                if (bot == null || bot.BotAi == null)
                    continue;
                bot.BotAi.OnUserLeaveRoom(session);
                if (bot.IsPet && bot.PetData.OwnerId == user.UserId && !_room.CheckRights(session, true))
                {
                    if (!petsToRemove.Contains(bot))
                        petsToRemove.Add(bot);
                }
            }
            foreach (var toRemove in petsToRemove.ToList())
            {
                if (toRemove == null)
                    continue;
                var userHabbo = GetHabbo(user);
                var pets = userHabbo?.Inventory?.Pets;
                if (pets == null)
                    continue;
                if (pets.AddPet(toRemove.PetData))
                {
                    toRemove.PetData.RoomId = 0;
                    toRemove.PetData.PlacedInRoom = false;
                    RemoveBot(toRemove.VirtualId, false);
                }
            }
            _room.GetGameMap().RemoveUserFromMap(user, new(user.X, user.Y));
        }
        catch (Exception e)
        {
            ExceptionLogger.LogCriticalException(e);
        }
    }

    private void RemoveRoomUser(RoomUser user)
    {
        if (user.SetStep)
            _room.GetGameMap().GameMap[user.SetX, user.SetY] = user.SqState;
        else
            _room.GetGameMap().GameMap[user.X, user.Y] = user.SqState;
        _room.GetGameMap().RemoveUserFromMap(user, new(user.X, user.Y));
        _room.SendPacket(new UserRemoveComposer(user.VirtualId));
        if (_users.TryRemove(user.InternalRoomId, out _))
        {
            //uhmm, could put the below stuff in but idk.
        }
        UserCount = _users.Count(x => !x.Value.IsBot);
        _room.UsersNow = UserCount;
        user.InternalRoomId = -1;
        OnRemove(user);
    }

    internal void ForceRemoveUser(RoomUser user)
    {
        if (user == null)
            return;

        RemoveRoomUser(user);
        user.Dispose();
    }

    public bool TryGetPet(int petId, out RoomUser? pet) => _pets.TryGetValue(petId, out pet);

    public bool TryGetBot(int botId, out RoomUser? bot) => _bots.TryGetValue(botId, out bot);

    public bool TryGetBotByName(string name, out RoomUser? bot)
    {
        bot = _bots.Values.FirstOrDefault(entry =>
            entry.BotData != null && entry.BotData.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return bot != null;
    }

    public void UpdateUserCount(int count)
    {
        UserCount = count;
        _room.UsersNow = count;
        using var connection = _database.Connection();
        connection.Execute("UPDATE `rooms` SET `users_now` = @count WHERE `id` = @roomId LIMIT 1", new { count = count, roomId = _room.RoomId });
    }

    public bool TryGetRoomUserByVirtualId(int virtualId, out RoomUser? user) => _users.TryGetValue(virtualId, out user);

    public bool TryGetRoomUserByHabbo(int id, out RoomUser? user)
    {
        user = GetUserList().FirstOrDefault(entry => GetHabbo(entry)?.Id == id);
        return user != null;
    }

    public List<RoomUser> GetRoomUsers() => GetUserList().Where(x => !x.IsBot).ToList();

    public List<RoomUser> GetRoomUserByRank(int minRank)
    {
        var returnList = new List<RoomUser>();
        foreach (var user in GetUserList().ToList())
        {
            var habbo = GetHabbo(user);
            if (user?.IsBot == false && habbo?.Rank >= minRank)
                returnList.Add(user);
        }
        return returnList;
    }

    public bool TryGetRoomUserByHabbo(string username, out RoomUser? user)
    {
        user = GetUserList().FirstOrDefault(entry =>
            GetHabbo(entry)?.Username.Equals(username, StringComparison.OrdinalIgnoreCase) == true);
        return user != null;
    }

    public void UpdatePets()
    {
        using var connection = _database.Connection();
        foreach (var pet in GetPets().ToList())
        {
            if (pet == null)
                continue;
            if (pet.DbState == PetDatabaseUpdateState.NeedsInsert)
            {
                connection.Execute("INSERT INTO `bots` (`id`,`user_id`,`room_id`,`name`,`x`,`y`,`z`) VALUES (@id, @ownerId, @roomId, @name, '0', '0', '0')", 
                    new { id = pet.PetId, ownerId = pet.OwnerId, roomId = pet.RoomId, name = pet.Name });
                
                connection.Execute(
                    "INSERT INTO `bots_petdata` (`type`,`race`,`color`,`experience`,`energy`,`createstamp`,`nutrition`,`respect`) VALUES (@type, @race, @color, '0', '100', @creationStamp, '0', '0')", 
                    new { type = pet.Type, race = pet.Race, color = pet.Color, creationStamp = pet.CreationStamp });
            }
            else if (pet.DbState == PetDatabaseUpdateState.NeedsUpdate)
            {
                //Surely this can be *99 better? // TODO
                TryGetRoomUserByVirtualId(pet.VirtualId, out var user);
                connection.Execute("UPDATE `bots` SET room_id = @roomId, x = @x, y = @y, z = @z WHERE `id` = @id LIMIT 1", 
                    new { roomId = pet.RoomId, x = user?.X ?? 0, y = user?.Y ?? 0, z = user?.Z ?? 0, id = pet.PetId });
                    
                connection.Execute(
                    "UPDATE `bots_petdata` SET `experience` = @experience, `energy` = @energy, `nutrition` = @nutrition, `respect` = @respect WHERE `id` = @id LIMIT 1", 
                    new { experience = pet.Experience, energy = pet.Energy, nutrition = pet.Nutrition, respect = pet.Respect, id = pet.PetId });
            }
            pet.DbState = PetDatabaseUpdateState.Updated;
        }
    }

    private void UpdateBots()
    {
        using var connection = _database.Connection();
        foreach (var user in GetRoomUsers().ToList())
        {
            if (user == null || !user.IsBot)
                continue;
            if (user.IsBot)
            {
                connection.Execute("UPDATE bots SET x=@x, y=@y, z=@z, name=@name, look=@look, rotation=@rotation WHERE id=@id LIMIT 1;", 
                    new { name = user.BotData.Name, look = user.BotData.Look, rotation = user.BotData.Rot, x = user.X, y = user.Y, z = user.Z, id = user.BotData.BotId });
            }
        }
    }


    public List<Pet> GetPets()
    {
        var pets = new List<Pet>();
        foreach (var user in _pets.Values.ToList())
        {
            if (user == null || !user.IsPet)
                continue;
            pets.Add(user.PetData);
        }
        return pets;
    }

    public void SerializeStatusUpdates()
    {
        var users = new List<RoomUser>();
        var roomUsers = GetUserList();
        if (roomUsers == null)
            return;
        foreach (var user in roomUsers.ToList())
        {
            if (user == null || !user.UpdateNeeded || users.Contains(user))
                continue;
            user.UpdateNeeded = false;
            users.Add(user);
        }
        if (users.Count > 0)
            _room.SendPacket(new UserUpdateComposer(users));
    }

    public void UpdateUserStatusses()
    {
        foreach (var user in GetUserList().ToList())
        {
            if (user == null)
                continue;
            UpdateUserStatus(user, false);
        }
    }

    private bool IsValid(RoomUser user)
    {
        if (user == null)
            return false;
        if (user.IsBot)
            return true;
        var habbo = GetHabbo(user);
        if (habbo == null || !habbo.IsInRoom(_room))
            return false;
        return true;
    }

    private Habbo? GetHabbo(RoomUser? user)
    {
        var client = user?.GetClient();
        return client?.GetHabbo();
    }

    public void OnCycle()
    {
        var userCounter = 0;
        try
        {
            var toRemove = new List<RoomUser>();
            foreach (var user in GetUserList().ToList())
            {
                if (user == null)
                    continue;
                if (!IsValid(user))
                {
                    var client = user.GetClient();
                    if (client != null)
                        _ = _room.GetRoomService().LeaveRoom(client, false);
                    else
                        RemoveRoomUser(user);
                }
                if (user.NeedsAutokick && !toRemove.Contains(user))
                {
                    toRemove.Add(user);
                    continue;
                }
                var updated = false;
                user.IdleTime++;
                user.HandleSpamTicks();
                if (!user.IsBot && !user.IsAsleep && user.IdleTime >= 600)
                {
                    user.IsAsleep = true;
                    _room.SendPacket(new SleepComposer(user, true));
                }
                if (user.CarryItemId > 0)
                {
                    user.CarryTimer--;
                    if (user.CarryTimer <= 0)
                        user.CarryItem(0);
                }
                if (_room.GotFreeze())
                    _room.GetFreeze().CycleUser(user);
                var invalidStep = false;
                if (user.IsRolling)
                {
                    if (user.RollerDelay <= 0)
                    {
                        UpdateUserStatus(user, false);
                        user.IsRolling = false;
                    }
                    else
                        user.RollerDelay--;
                }
                if (user.SetStep)
                {
                    var gameMap = _room.GetGameMap();
                    if (gameMap.IsValidStep2(user, new(user.X, user.Y), new(user.SetX, user.SetY), user.GoalX == user.SetX && user.GoalY == user.SetY, user.AllowOverride))
                    {
                        if (!user.RidingHorse)
                            gameMap.UpdateUserMovement(new(user.Coordinate.X, user.Coordinate.Y), new(user.SetX, user.SetY), user);
                        var coordinatedItems = gameMap.GetCoordinatedItems(new(user.X, user.Y));
                        foreach (var item in coordinatedItems.ToList()) item.UserWalksOffFurni(user);
                        if (!user.IsBot)
                        {
                            user.X = user.SetX;
                            user.Y = user.SetY;
                            user.Z = user.SetZ;
                        }
                        else if (user.IsBot && !user.RidingHorse)
                        {
                            user.X = user.SetX;
                            user.Y = user.SetY;
                            user.Z = user.SetZ;
                        }
                        if (!user.IsBot && user.RidingHorse)
                        {
                            if (TryGetRoomUserByVirtualId(user.HorseId, out var horse) && horse != null)
                            {
                                horse.X = user.SetX;
                                horse.Y = user.SetY;
                            }
                        }
                        if (user.X == gameMap.Model.DoorX && user.Y == gameMap.Model.DoorY && !toRemove.Contains(user) && !user.IsBot)
                        {
                            toRemove.Add(user);
                            continue;
                        }
                        var items = gameMap.GetCoordinatedItems(new(user.X, user.Y));
                        foreach (var item in items.ToList()) item.UserWalksOnFurni(user);
                        UpdateUserStatus(user, true);
                    }
                    else
                        invalidStep = true;
                    user.SetStep = false;
                }
                if (user.PathRecalcNeeded)
                {
                    if (user.Path.Count > 1)
                        user.Path.Clear();
                    user.Path = PathFinder.FindPath(user, _room.GetGameMap().DiagonalEnabled, _room.GetGameMap(), new(user.X, user.Y), new(user.GoalX, user.GoalY));
                    if (user.Path.Count > 1)
                    {
                        user.PathStep = 1;
                        user.IsWalking = true;
                        user.PathRecalcNeeded = false;
                    }
                    else
                    {
                        user.PathRecalcNeeded = false;
                        if (user.Path.Count > 1)
                            user.Path.Clear();
                        Log.Debug("Path recalculation produced no usable path. RoomId={roomId}, UserId={userId}, VirtualId={virtualId}, Current=({x},{y}), Goal=({goalX},{goalY})",
                            _room.RoomId, user.HabboId, user.VirtualId, user.X, user.Y, user.GoalX, user.GoalY);
                    }
                }
                if (user.IsWalking && !user.Freezed)
                {
                    if (invalidStep || user.PathStep >= user.Path.Count || user.GoalX == user.X && user.GoalY == user.Y) //No path found, or reached goal (:
                    {
                        if (invalidStep)
                        {
                            Log.Debug("Walking stopped due to invalid step. RoomId={roomId}, UserId={userId}, VirtualId={virtualId}, Current=({x},{y}), Goal=({goalX},{goalY}), PathStep={pathStep}, PathCount={pathCount}",
                                _room.RoomId, user.HabboId, user.VirtualId, user.X, user.Y, user.GoalX, user.GoalY, user.PathStep, user.Path.Count);
                        }
                        user.IsWalking = false;
                        user.RemoveStatus("mv");
                        if (user.Statusses.ContainsKey("sign"))
                            user.RemoveStatus("sign");
                        if (user.IsBot && user.BotData.TargetUser > 0)
                        {
                            if (user.CarryItemId > 0)
                            {
                                if (_room.GetRoomUserManager().TryGetRoomUserByHabbo(user.BotData.TargetUser, out var target) &&
                                    target != null &&
                                    Gamemap.TilesTouching(user.X, user.Y, target.X, target.Y))
                                {
                                    user.SetRot(Rotation.Calculate(user.X, user.Y, target.X, target.Y), false);
                                    target.SetRot(Rotation.Calculate(target.X, target.Y, user.X, user.Y), false);
                                    target.CarryItem(user.CarryItemId);
                                }
                            }
                            user.CarryItem(0);
                            user.BotData.TargetUser = 0;
                        }
                        if (user.RidingHorse && user.IsPet == false && !user.IsBot)
                        {
                            if (TryGetRoomUserByVirtualId(user.HorseId, out var mascotaVinculada) && mascotaVinculada != null)
                            {
                                mascotaVinculada.IsWalking = false;
                                mascotaVinculada.RemoveStatus("mv");
                                mascotaVinculada.UpdateNeeded = true;
                            }
                        }
                    }
                    else
                    {
                        var gameMap = _room.GetGameMap();
                        var nextStep = user.Path[user.Path.Count - user.PathStep - 1];
                        user.PathStep++;
                        if (user.FastWalking && user.PathStep < user.Path.Count)
                        {
                            var s2 = user.Path.Count - user.PathStep - 1;
                            nextStep = user.Path[s2];
                            user.PathStep++;
                        }
                        if (user.SuperFastWalking && user.PathStep < user.Path.Count)
                        {
                            var s2 = user.Path.Count - user.PathStep - 1;
                            nextStep = user.Path[s2];
                            user.PathStep++;
                            user.PathStep++;
                        }
                        var nextX = nextStep.X;
                        var nextY = nextStep.Y;
                        user.RemoveStatus("mv");
                        if (gameMap.IsValidStep2(user, new(user.X, user.Y), new(nextX, nextY), user.GoalX == nextX && user.GoalY == nextY, user.AllowOverride))
                        {
                            var nextZ = gameMap.SqAbsoluteHeight(nextX, nextY);
                            if (!user.IsBot)
                            {
                                if (user.IsSitting)
                                {
                                    user.Statusses.Remove("sit");
                                    user.Z += 0.35;
                                    user.IsSitting = false;
                                    user.UpdateNeeded = true;
                                }
                                else if (user.IsLying)
                                {
                                    user.Statusses.Remove("sit");
                                    user.Z += 0.35;
                                    user.IsLying = false;
                                    user.UpdateNeeded = true;
                                }
                            }
                            if (!user.IsBot)
                            {
                                user.Statusses.Remove("lay");
                                user.Statusses.Remove("sit");
                            }
                            if (!user.IsBot && !user.IsPet)
                            {
                                var habbo = GetHabbo(user);
                                if (habbo?.IsTeleporting == true)
                                {
                                    habbo.IsTeleporting = false;
                                    habbo.TeleporterId = 0;
                                }
                                else if (habbo?.IsHopping == true)
                                {
                                    habbo.IsHopping = false;
                                    habbo.HopperId = 0;
                                }
                            }
                            if (!user.IsBot && user.RidingHorse && user.IsPet == false)
                            {
                                if (TryGetRoomUserByVirtualId(user.HorseId, out var horse) && horse != null)
                                {
                                    horse.SetStatus("mv", $"{nextX},{nextY},{TextHandling.GetString(nextZ)}");
                                    horse.UpdateNeeded = true;
                                }
                                user.SetStatus("mv", $"{+nextX},{nextY},{TextHandling.GetString(nextZ + 1)}");
                                user.UpdateNeeded = true;
                            }
                            else
                                user.SetStatus("mv", $"{nextX},{nextY},{TextHandling.GetString(nextZ)}");
                            var newRot = Rotation.Calculate(user.X, user.Y, nextX, nextY, user.MoonwalkEnabled);
                            user.RotBody = newRot;
                            user.RotHead = newRot;
                            user.SetStep = true;
                            user.SetX = nextX;
                            user.SetY = nextY;
                            user.SetZ = nextZ;
                            UpdateUserEffect(user, user.SetX, user.SetY);
                            updated = true;
                            if (user.RidingHorse && user.IsPet == false && !user.IsBot)
                            {
                                if (TryGetRoomUserByVirtualId(user.HorseId, out var horse) && horse != null)
                                {
                                    horse.RotBody = newRot;
                                    horse.RotHead = newRot;
                                    horse.SetStep = true;
                                    horse.SetX = nextX;
                                    horse.SetY = nextY;
                                    horse.SetZ = nextZ;
                                }
                            }
                            gameMap.GameMap[user.X, user.Y] = user.SqState; // REstore the old one
                            user.SqState = gameMap.GameMap[user.SetX, user.SetY]; //Backup the new one
                            if (!_room.RoomBlockingEnabled)
                            {
                                if (_room.GetRoomUserManager().TryGetUserForSquare(nextX, nextY, out var users) && users != null)
                                    gameMap.GameMap[nextX, nextY] = 0;
                            }
                            else
                                gameMap.GameMap[nextX, nextY] = 1;
                        }
                        else
                        {
                            Log.Debug("Next walking step rejected. RoomId={roomId}, UserId={userId}, VirtualId={virtualId}, Current=({x},{y}), Next=({nextX},{nextY}), Goal=({goalX},{goalY}), AllowOverride={allowOverride}",
                                _room.RoomId, user.HabboId, user.VirtualId, user.X, user.Y, nextX, nextY, user.GoalX, user.GoalY, user.AllowOverride);
                        }
                    }
                    if (!user.RidingHorse)
                        user.UpdateNeeded = true;
                }
                else
                {
                    if (user.Statusses.ContainsKey("mv"))
                    {
                        user.RemoveStatus("mv");
                        user.UpdateNeeded = true;
                        if (user.RidingHorse)
                        {
                            if (TryGetRoomUserByVirtualId(user.HorseId, out var horse) && horse != null)
                            {
                                horse.RemoveStatus("mv");
                                horse.UpdateNeeded = true;
                            }
                        }
                    }
                }

                if (user.RidingHorse)
                    user.ApplyEffect(77);

                if (user.IsBot && user.BotAi != null)
                    user.BotAi.OnTimerTick();
                else
                    userCounter++;

                if (!updated) UpdateUserEffect(user, user.X, user.Y);
            }

            foreach (var userToRemove in toRemove.ToList())
            {
                var client = _clientManager.GetClientByUserId(userToRemove.HabboId);
                if (client != null)
                    _ = _room.GetRoomService().LeaveRoom(client, true);
                else
                    RemoveRoomUser(userToRemove);
            }

            if (UserCount != userCounter)
                UpdateUserCount(userCounter);
        }
        catch (Exception e)
        {
            ExceptionLogger.LogCriticalException(e);
        }
    }

    public void UpdateUserStatus(RoomUser user, bool cyclegameitems)
    {
        if (user == null)
            return;
        try
        {
            var isBot = user.IsBot;
            if (isBot)
                cyclegameitems = false;
            if (UnixTimestamp.GetNow() > UnixTimestamp.GetNow() + user.SignTime)
            {
                if (user.Statusses.ContainsKey("sign"))
                {
                    user.Statusses.Remove("sign");
                    user.UpdateNeeded = true;
                }
            }
            if (user.Statusses.ContainsKey("lay") && !user.IsLying || user.Statusses.ContainsKey("sit") && !user.IsSitting)
            {
                if (user.Statusses.ContainsKey("lay"))
                    user.Statusses.Remove("lay");
                if (user.Statusses.ContainsKey("sit"))
                    user.Statusses.Remove("sit");
                user.UpdateNeeded = true;
            }
            else if (user.IsLying || user.IsSitting)
                return;
            double newZ;
            var itemsOnSquare = _room.GetGameMap().GetAllRoomItemForSquare(user.X, user.Y);
            if (itemsOnSquare.Count != 0)
            {
                if (user.RidingHorse && user.IsPet == false)
                    newZ = _room.GetGameMap().SqAbsoluteHeight(user.X, user.Y, itemsOnSquare.ToList()) + 1;
                else
                    newZ = _room.GetGameMap().SqAbsoluteHeight(user.X, user.Y, itemsOnSquare.ToList());
            }
            else
                newZ = _room.GetGameMap().Model.SqFloorHeight[user.X, user.Y];
            if (Math.Abs(newZ - user.Z) > 0.001)
            {
                user.Z = newZ;
                user.UpdateNeeded = true;
            }
            var model = _room.GetGameMap().Model;
            if (model.SqState[user.X, user.Y] == SquareState.Seat)
            {
                if (!user.Statusses.ContainsKey("sit"))
                    user.Statusses.Add("sit", "1.0");
                user.Z = model.SqFloorHeight[user.X, user.Y];
                user.RotHead = model.SqSeatRot[user.X, user.Y];
                user.RotBody = model.SqSeatRot[user.X, user.Y];
                user.UpdateNeeded = true;
            }
            if (itemsOnSquare.Count == 0)
                user.LastItem = null;
            foreach (var item in itemsOnSquare.ToList())
            {
                if (item == null)
                    continue;
                var definition = item.Definition;
                if (definition == null)
                    continue;
                if (definition.IsSeat)
                {
                        if (!user!.Statusses.ContainsKey("sit"))
                        {
                            if (!user.Statusses.ContainsKey("sit"))
                                user.Statusses.Add("sit", TextHandling.GetString(definition.Height));
                    }
                    user.Z = item.GetZ;
                    user.RotHead = item.Rotation;
                    user.RotBody = item.Rotation;
                    user.UpdateNeeded = true;
                }
                switch (definition.InteractionType)
                {
                    case var _ when definition.IsBedLike:
                        {
                            if (!user!.Statusses.ContainsKey("lay"))
                                user.Statusses.Add("lay", $"{TextHandling.GetString(definition.Height)} null");
                            user.Z = item.GetZ;
                            user.RotHead = item.Rotation;
                            user.RotBody = item.Rotation;
                            user.UpdateNeeded = true;
                            break;
                        }
                    case var _ when definition.IsBanzaiGate:
                        {
                            if (cyclegameitems)
                            {
                                var habbo = GetHabbo(user);
                                var effects = habbo?.Effects;
                                var t = _room.GetTeamManagerForBanzai();
                                if (effects == null || t == null)
                                    break;
                                var effectId = Convert.ToInt32(item.Team + 32);
                                if (user!.Team == Team.None)
                                {
                                    if (t.CanEnterOnTeam(item.Team))
                                    {
                                        if (user.Team != Team.None)
                                            t.OnUserLeave(user);
                                        user.Team = item.Team;
                                        t.AddUser(user);
                                        if (effects.CurrentEffect != effectId)
                                            effects.ApplyEffect(effectId);
                                    }
                                }
                                else if (user.Team != Team.None && user.Team != item.Team)
                                {
                                    t.OnUserLeave(user);
                                    user.Team = Team.None;
                                    effects.ApplyEffect(0);
                                }
                                else
                                {
                                    //usersOnTeam--;
                                    t.OnUserLeave(user);
                                    if (effects.CurrentEffect == effectId)
                                        effects.ApplyEffect(0);
                                    user.Team = Team.None;
                                }
                                //Item.ExtraData = usersOnTeam.ToString();
                                //Item.UpdateState(false, true);
                            }
                            break;
                        }
                    case var _ when definition.IsFreezeGate:
                        {
                            if (cyclegameitems)
                            {
                                var habbo = GetHabbo(user);
                                var effects = habbo?.Effects;
                                var t = _room.GetTeamManagerForFreeze();
                                if (effects == null || t == null)
                                    break;
                                var effectId = Convert.ToInt32(item.Team + 39);
                                if (user!.Team == Team.None)
                                {
                                    if (t.CanEnterOnTeam(item.Team))
                                    {
                                        if (user.Team != Team.None)
                                            t.OnUserLeave(user);
                                        user.Team = item.Team;
                                        t.AddUser(user);
                                        if (effects.CurrentEffect != effectId)
                                            effects.ApplyEffect(effectId);
                                    }
                                }
                                else if (user.Team != Team.None && user.Team != item.Team)
                                {
                                    t.OnUserLeave(user);
                                    user.Team = Team.None;
                                    effects.ApplyEffect(0);
                                }
                                else
                                {
                                    //usersOnTeam--;
                                    t.OnUserLeave(user);
                                    if (effects.CurrentEffect == effectId)
                                        effects.ApplyEffect(0);
                                    user.Team = Team.None;
                                }
                                //Item.ExtraData = usersOnTeam.ToString();
                                //Item.UpdateState(false, true);
                            }
                            break;
                        }
                    case var _ when definition.IsBanzaiTeleport:
                        {
                            if (user!.Statusses.ContainsKey("mv"))
                                _room.GetGameItemHandler().OnTeleportRoomUserEnter(user, item);
                            break;
                        }
                    case var _ when definition.IsEffectProviderFurni:
                        {
                            if (user == null)
                                return;
                            if (!user.IsBot)
                            {
                                var effects = GetHabbo(user)?.Effects;
                                if (item == null || item.Definition == null || effects == null)
                                    return;
                                if (definition.EffectId == 0 && effects.CurrentEffect == 0)
                                    return;
                                effects.ApplyEffect(definition.EffectId);
                                item.LegacyDataString = "1";
                                item.UpdateState(false, true);
                                item.RequestUpdate(2, true);
                            }
                            break;
                        }
                    case var _ when definition.IsArrow:
                        {
                            if (user!.GoalX == item.GetX && user.GoalY == item.GetY)
                            {
                                var habbo = GetHabbo(user);
                                if (habbo == null || !habbo.TryGetCurrentRoom(out var room))
                                    continue;
                                if (!_room.GetItemTeleporterFinder().IsTeleLinked(item.Id, room))
                                    user.UnlockWalking();
                                else
                                {
                                    var linkedTele = _room.GetItemTeleporterFinder().GetLinkedTele(item.Id);
                                    var teleRoomId = _room.GetItemTeleporterFinder().GetTeleRoomId(linkedTele, room);
                                    if (teleRoomId == room.RoomId)
                                    {
                                        var targetItem = room.GetRoomItemHandler().GetItem(linkedTele);
                                        if (targetItem == null)
                                        {
                                            user.GetClient()?.SendWhisper(_room.GetLanguageManager().Require("room.teleport.arrow.invalid"));
                                            return;
                                        }
                                        room.GetGameMap().TeleportToItem(user, targetItem);
                                    }
                                    else if (teleRoomId != room.RoomId)
                                    {
                                        if (user != null && !user.IsBot)
                                        {
                                            habbo.IsTeleporting = true;
                                            habbo.TeleportingRoomId = teleRoomId;
                                            habbo.TeleporterId = linkedTele;
                                            _ = _room.GetRoomService().PrepareRoom(user.GetClient()!, teleRoomId, "");
                                        }
                                    }
                                    else if (_room.GetRoomItemHandler().GetItem(linkedTele) != null)
                                    {
                                        user.SetPos(item.GetX, item.GetY, item.GetZ);
                                        user.SetRot(item.Rotation, false);
                                    }
                                    else
                                        user.UnlockWalking();
                                }
                            }
                            break;
                        }
                }
            }
            if (user!.IsSitting && user.TeleportEnabled)
            {
                user.Z -= 0.35;
                user.UpdateNeeded = true;
            }
            if (cyclegameitems)
            {
                if (_room.GotSoccer())
                    _room.GetSoccer().OnUserWalk(user);
                if (_room.GotBanzai())
                    _room.GetBanzai().OnUserWalk(user);
                if (_room.GotFreeze())
                    _room.GetFreeze().OnUserWalk(user);
            }
        }
        catch (Exception e)
        {
            ExceptionLogger.LogException(e);
        }
    }

    private void UpdateUserEffect(RoomUser user, int x, int y)
    {
        if (user == null || user.IsBot)
            return;
        var habbo = GetHabbo(user);
        var effects = habbo?.Effects;
        if (effects == null)
            return;
        try
        {
            var newCurrentUserItemEffect = _room.GetGameMap().EffectMap[x, y];
            if (newCurrentUserItemEffect > 0)
            {
                if (effects.CurrentEffect == 0)
                    user.CurrentItemEffect = ItemEffectType.None;
                var type = ByteToItemEffectEnum.Parse(newCurrentUserItemEffect);
                if (type != user.CurrentItemEffect)
                {
                    switch (type)
                    {
                        case ItemEffectType.Iceskates:
                            {
                                effects.ApplyEffect(habbo?.Gender == "M" ? 38 : 39);
                                user.CurrentItemEffect = ItemEffectType.Iceskates;
                                break;
                            }
                        case ItemEffectType.Normalskates:
                            {
                                effects.ApplyEffect(habbo?.Gender == "M" ? 55 : 56);
                                user.CurrentItemEffect = type;
                                break;
                            }
                        case ItemEffectType.Swim:
                            {
                                effects.ApplyEffect(29);
                                user.CurrentItemEffect = type;
                                break;
                            }
                        case ItemEffectType.SwimLow:
                            {
                                effects.ApplyEffect(30);
                                user.CurrentItemEffect = type;
                                break;
                            }
                        case ItemEffectType.SwimHalloween:
                            {
                                effects.ApplyEffect(37);
                                user.CurrentItemEffect = type;
                                break;
                            }
                        case ItemEffectType.None:
                            {
                                effects.ApplyEffect(-1);
                                user.CurrentItemEffect = type;
                                break;
                            }
                    }
                }
            }
            else if (user.CurrentItemEffect != ItemEffectType.None && newCurrentUserItemEffect == 0)
            {
                effects.ApplyEffect(-1);
                user.CurrentItemEffect = ItemEffectType.None;
            }
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to apply item effects for room user. RoomId={roomId}, UserId={userId}", _room.RoomId, user.UserId);
        }
    }

    public ICollection<RoomUser> GetUserList() => _users.Values;

    public void Dispose()
    {
        UpdatePets();
        UpdateBots();
        _room.UsersNow = 0;
        using (var connection = _database.Connection())
        {
            connection.Execute("UPDATE `rooms` SET `users_now` = '0' WHERE `id` = @roomId LIMIT 1", new { roomId = _room.Id });
        }
        _users.Clear();
        _pets.Clear();
        _bots.Clear();
        UserCount = 0;
        PetCount = 0;
    }
}
