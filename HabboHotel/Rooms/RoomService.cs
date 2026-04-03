using Dapper;
using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.Communication.Packets.Outgoing.Rooms.Settings;
using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.Communication.Packets.Outgoing.Notifications;
using Plus.Core.Language;
using Plus.Database;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Navigator;
using Plus.HabboHotel.Rooms.Chat.Filter;
using Plus.Utilities;
using Microsoft.Extensions.Logging;

namespace Plus.HabboHotel.Rooms;

public class RoomService : IRoomService
{
    private readonly IRoomManager _roomManager;
    private readonly IRoomFactory _roomFactory;
    private readonly INavigatorManager _navigatorManager;
    private readonly IWordFilterManager _wordFilterManager;
    private readonly ILanguageManager _languageManager;
    private readonly IAchievementService _achievementService;
    private readonly IDatabase _database;
    private readonly ILogger<RoomService> _logger;

    public RoomService(
        IRoomManager roomManager,
        IRoomFactory roomFactory,
        INavigatorManager navigatorManager,
        IWordFilterManager wordFilterManager,
        ILanguageManager languageManager,
        IAchievementService achievementService,
        IDatabase database,
        ILogger<RoomService> logger)
    {
        _roomManager = roomManager;
        _roomFactory = roomFactory;
        _navigatorManager = navigatorManager;
        _wordFilterManager = wordFilterManager;
        _languageManager = languageManager;
        _achievementService = achievementService;
        _database = database;
        _logger = logger;
    }

    public async Task PrepareRoom(GameClient session, uint roomId, string password)
    {
        if (!TryGetPreparingHabbo(session, out var habbo))
            return;

        _logger.LogInformation("PrepareRoom start. SessionId={sessionId}, UserId={userId}, Username={username}, TargetRoomId={roomId}, InRoom={inRoom}, Teleporting={teleporting}, Hopping={hopping}",
            session.Id, habbo.Id, habbo.Username, roomId, habbo.TryGetCurrentRoom(out _), habbo.IsTeleporting, habbo.IsHopping);
        if (habbo.TryGetCurrentRoom(out _))
            await LeaveRoomInternal(session, habbo, false, false, "PrepareNextRoom");

        if (habbo.IsTeleporting && habbo.TeleportingRoomId != roomId)
        {
            session.Send(new CloseConnectionComposer());
            return;
        }

        if (!_roomManager.TryLoadRoom(roomId, out var room))
        {
            _logger.LogWarning("PrepareRoom failed: room could not be loaded. SessionId={sessionId}, RoomId={roomId}", session.Id, roomId);
            session.Send(new CloseConnectionComposer());
            return;
        }

        if (room.IsCrashed)
        {
            _logger.LogWarning("PrepareRoom failed: room is crashed. SessionId={sessionId}, RoomId={roomId}", session.Id, roomId);
            session.SendNotification(_languageManager.Require("room.crashed.enter"));
            session.Send(new CloseConnectionComposer());
            return;
        }

        if (room.GetRoomUserManager().UserCount >= room.UsersMax && !(habbo.Permissions?.HasRight("room_enter_full") ?? false) && habbo.Id != room.OwnerId)
        {
            _logger.LogWarning("PrepareRoom failed: room is full. SessionId={sessionId}, RoomId={roomId}, UserCount={userCount}, UsersMax={usersMax}", session.Id, roomId, room.GetRoomUserManager().UserCount, room.UsersMax);
            session.Send(new CantConnectComposer(1));
            session.Send(new CloseConnectionComposer());
            return;
        }

        if (!(habbo.Permissions?.HasRight("room_ban_override") ?? false) && room.GetBans().IsBanned(habbo.Id))
        {
            habbo.RoomAuthOk = false;
            _logger.LogWarning("PrepareRoom failed: user is banned from room. SessionId={sessionId}, UserId={userId}, RoomId={roomId}", session.Id, habbo.Id, roomId);
            session.Send(new CantConnectComposer(4));
            session.Send(new CloseConnectionComposer());
            return;
        }

        if (!await TryAuthorizeRoomEntry(session, habbo, room, password))
            return;
    }

    public async Task<RoomData?> CreateRoom(GameClient session, string name, string description, string modelName, int category, int maxVisitors, int tradeSettings)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return null;

        var rooms = _roomFactory.GetRoomsDataByOwnerSortByName(habbo.Id);
        if (rooms.Count >= 500)
        {
            session.Send(new CanCreateRoomComposer(true, 500));
            return null;
        }

        var filteredName = _wordFilterManager.CheckMessage(name);
        var filteredDescription = _wordFilterManager.CheckMessage(description);
        if (filteredName.Length is < 3 or > 25)
            return null;

        if (!_roomManager.TryGetModel(modelName, out var model) || model == null)
            return null;

        if (!_navigatorManager.TryGetSearchResultList(category, out var searchResultList) || searchResultList == null)
            category = 36;
        else if (searchResultList.CategoryType != NavigatorCategoryType.Category || searchResultList.RequiredRank > habbo.Rank)
            category = 36;

        if (maxVisitors is < 10 or > 25)
            maxVisitors = 10;
        if (tradeSettings is < 0 or > 2)
            tradeSettings = 0;

        var newRoom = _roomManager.CreateRoom(session, filteredName, filteredDescription, category, maxVisitors, tradeSettings, model);
        if (newRoom != null)
        {
            session.Send(new FlatCreatedComposer(newRoom.Id, filteredName));
            habbo.Messenger?.NotifyChangesToFriends();
        }

        return newRoom;
    }

    public async Task EnterRoom(GameClient session)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.TryGetCurrentRoom(out var room))
            return;

        session.Send(new RoomReadyComposer(room.RoomId, room.ModelName));
        if (room.Wallpaper != "0.0")
            session.Send(new RoomPropertyComposer("wallpaper", room.Wallpaper));
        if (room.Floor != "0.0")
            session.Send(new RoomPropertyComposer("floor", room.Floor));

        session.Send(new RoomPropertyComposer("landscape", room.Landscape));
        session.Send(new RoomRatingComposer(room.Score, !(habbo.RatedRooms.Contains(room.RoomId) || room.OwnerId == habbo.Id)));

        using (var connection = _database.Connection())
        {
            await connection.ExecuteAsync(
                "INSERT INTO user_roomvisits (user_id, room_id, entry_timestamp, exit_timestamp) VALUES (@userId, @roomId, @entryTimestamp, @exitTimestamp)",
                new
                {
                    userId = habbo.Id,
                    roomId = room.RoomId,
                    entryTimestamp = UnixTimestamp.GetNow(),
                    exitTimestamp = 0
                });
        }

        if (room.OwnerId != habbo.Id)
        {
            habbo.HabboStats.RoomVisits += 1;
            await _achievementService.ProgressAchievement(session, "ACH_RoomEntry", 1);
        }
    }

    public async Task<bool> FinalizeRoomEntry(GameClient session)
    {
        if (!TryGetPreparingHabbo(session, out var habbo) || !habbo.TryGetCurrentRoom(out var room))
            return false;

        _logger.LogInformation("[RoomFlow] User={userId} Action=Join Room={roomId} Step=FinalizeEntryStart Session={sessionId}",
            habbo.Id, room.RoomId, session.Id);

        if (room.IsDisposed || room.IsUnloading)
        {
            habbo.LeaveRoom();
            _logger.LogWarning("[RoomFlow] User={userId} Action=Join Room={roomId} Step=FinalizeEntryRejected Reason=RoomUnavailable Session={sessionId}",
                habbo.Id, room.RoomId, session.Id);
            return false;
        }

        if (!room.TryAddHabboToRuntime(session))
        {
            _logger.LogWarning("[RoomFlow] User={userId} Action=Join Room={roomId} Step=FinalizeEntryFailed Session={sessionId}",
                habbo.Id, room.RoomId, session.Id);
            await LeaveRoomInternal(session, habbo, false, false, "FinalizeEntryFailed");
            return false;
        }

        _logger.LogInformation("[RoomFlow] User={userId} Action=Join Room={roomId} Step=FinalizeEntryCompleted Session={sessionId}",
            habbo.Id, room.RoomId, session.Id);
        return true;
    }

    public Task LeaveRoom(GameClient session, bool notifyUser = true)
    {
        if (!TryGetPreparingHabbo(session, out var habbo))
            return Task.CompletedTask;

        return LeaveRoomInternal(session, habbo, notifyUser, false, "Leave");
    }

    public Task KickFromRoom(GameClient session, bool notifyUser = true)
    {
        if (!TryGetPreparingHabbo(session, out var habbo))
            return Task.CompletedTask;

        return LeaveRoomInternal(session, habbo, notifyUser, true, "Kick");
    }

    public Task HandleDisconnect(GameClient session)
    {
        if (!TryGetPreparingHabbo(session, out var habbo))
            return Task.CompletedTask;

        return LeaveRoomInternal(session, habbo, false, false, "Disconnect");
    }

    private static bool TryGetPreparingHabbo(GameClient session, out Users.Habbo habbo)
    {
        habbo = session.GetHabbo();
        return habbo != null;
    }

    private Task LeaveRoomInternal(GameClient session, Users.Habbo habbo, bool notifyUser, bool notifyKick, string flow)
    {
        if (!habbo.TryGetCurrentRoom(out var room))
        {
            habbo.LeaveRoom();
            return Task.CompletedTask;
        }

        _logger.LogInformation("[RoomFlow] User={userId} Action=Leave Room={roomId} Step=Begin Flow={flow} NotifyUser={notifyUser} NotifyKick={notifyKick} Session={sessionId}",
            habbo.Id, room.RoomId, flow, notifyUser, notifyKick, session.Id);

        if (room.IsDisposed)
        {
            habbo.LeaveRoom();
            _logger.LogInformation("[RoomFlow] User={userId} Action=Leave Room={roomId} Step=DisposedRoomReferenceCleared Flow={flow} Session={sessionId}",
                habbo.Id, room.RoomId, flow, session.Id);
            return Task.CompletedTask;
        }

        room.TryRemoveHabboFromRuntime(session, notifyUser, notifyKick);
        _roomManager.NotifyRoomStateChanged(room);
        _logger.LogInformation("[RoomFlow] User={userId} Action=Leave Room={roomId} Step=Completed Flow={flow} Session={sessionId}",
            habbo.Id, room.RoomId, flow, session.Id);
        return Task.CompletedTask;
    }

    private async Task<bool> TryAuthorizeRoomEntry(GameClient session, Users.Habbo habbo, Room room, string password)
    {
        habbo.RoomAuthOk = false;

        if (room.Type == "public")
            return await TryAuthorizePublicRoomEntry(session, habbo, room);

        if (CanBypassPrivateRoomChecks(habbo, room))
            return await OpenAuthorizedRoom(session, habbo, room);

        if (room.Access == RoomAccess.Doorbell)
            return TryAuthorizeDoorbellRoomEntry(session, habbo, room);

        if (room.Access == RoomAccess.Password)
            return await TryAuthorizePasswordRoomEntry(session, habbo, room, password);

        return await OpenAuthorizedRoom(session, habbo, room);
    }

    private async Task<bool> TryAuthorizePublicRoomEntry(GameClient session, Users.Habbo habbo, Room room)
    {
        if (room.Access == RoomAccess.Doorbell && !(habbo.Permissions?.HasRight("room_enter_any_room") ?? false))
        {
            session.Send(new CantConnectComposer(1));
            session.Send(new CloseConnectionComposer());
            return false;
        }

        return await OpenAuthorizedRoom(session, habbo, room);
    }

    private static bool CanBypassPrivateRoomChecks(Users.Habbo habbo, Room room)
    {
        return habbo.Id == room.OwnerId ||
               (habbo.Permissions?.HasRight("room_enter_any_room") ?? false) ||
               (habbo.Permissions?.HasRight("room_any_owner") ?? false);
    }

    private static bool TryAuthorizeDoorbellRoomEntry(GameClient session, Users.Habbo habbo, Room room)
    {
        if (room.GetRoomUserManager().GetRoomUserByRank(2).Count > 0)
        {
            session.Send(new DoorbellComposer(""));
            room.SendPacket(new DoorbellComposer(habbo.Username), true);
        }
        else
        {
            session.Send(new CantConnectComposer(2));
            session.Send(new CloseConnectionComposer());
        }

        return false;
    }

    private async Task<bool> TryAuthorizePasswordRoomEntry(GameClient session, Users.Habbo habbo, Room room, string password)
    {
        if (password.ToLower() == room.Password.ToLower() || habbo.RoomAuthOk)
            return await OpenAuthorizedRoom(session, habbo, room);

        session.Send(new GenericErrorComposer(-100002));
        session.Send(new CloseConnectionComposer());
        return false;
    }

    private async Task<bool> OpenAuthorizedRoom(GameClient session, Users.Habbo habbo, Room room)
    {
        habbo.RoomAuthOk = true;
        habbo.EnterRoom(room);
        session.Send(new OpenConnectionComposer());
        _logger.LogInformation("OpenAuthorizedRoom completed. SessionId={sessionId}, UserId={userId}, Username={username}, RoomId={roomId}. Immediately entering prepared room.", session.Id, habbo.Id, habbo.Username, room.RoomId);
        await EnterRoom(session);
        return true;
    }
}
