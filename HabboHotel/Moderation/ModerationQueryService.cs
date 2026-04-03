using Dapper;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Core.Language;
using Plus.Database;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat.Logs;
using Plus.Utilities;

namespace Plus.HabboHotel.Moderation;

public class ModerationQueryService : IModerationQueryService
{
    private readonly IDatabase _database;
    private readonly IRoomFactory _roomFactory;
    private readonly IRoomManager _roomManager;
    private readonly ICacheManager _cacheManager;
    private readonly IGameClientManager _clientManager;
    private readonly IModerationManager _moderationManager;
    private readonly ILanguageManager _languageManager;

    public ModerationQueryService(
        IDatabase database,
        IRoomFactory roomFactory,
        IRoomManager roomManager,
        ICacheManager cacheManager,
        IGameClientManager clientManager,
        IModerationManager moderationManager,
        ILanguageManager languageManager)
    {
        _database = database;
        _roomFactory = roomFactory;
        _roomManager = roomManager;
        _cacheManager = cacheManager;
        _clientManager = clientManager;
        _moderationManager = moderationManager;
        _languageManager = languageManager;
    }

    public async Task GetUserInfo(GameClient session, int userId)
    {
        using var connection = _database.Connection();
        var user = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT `id` AS Id, `username` AS Username, `look` AS Look, `account_created` AS AccountCreated, `last_online` AS LastOnline, `mail` AS Mail FROM `users` WHERE `id` = @userId LIMIT 1",
            new { userId });

        if (user == null)
        {
            session.SendNotification(_languageManager.Require("moderation.user_info.not_found"));
            return;
        }

        var info = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT `cfhs` AS Cfhs, `cfhs_abusive` AS CfhsAbusive, `cautions` AS Cautions, `bans` AS Bans, `trading_locked` AS TradingLocked, `trading_locks_count` AS TradingLocksCount FROM `user_info` WHERE `user_id` = @userId LIMIT 1",
            new { userId });

        var isOnline = _clientManager.GetClientByUserId(userId) != null;
        session.Send(new ModeratorUserInfoComposer(user, info, isOnline));
    }

    public async Task GetRoomInfo(GameClient session, uint roomId)
    {
        if (!_roomFactory.TryGetData(roomId, out var data))
            return;

        var ownerInRoom = _roomManager.TryGetRoom(roomId, out var room) && room.GetRoomUserManager().GetRoomUserByHabbo(data!.OwnerId) != null;
        session.Send(new ModeratorRoomInfoComposer(data!, ownerInRoom));
    }

    public async Task GetUserRoomVisits(GameClient session, int userId)
    {
        var habbo = _cacheManager.GenerateUser(userId);
        if (habbo == null)
            return;

        using var connection = _database.Connection();
        var visitsRows = await connection.QueryAsync<dynamic>(
            "SELECT `room_id`, `entry_timestamp` FROM `user_roomvisits` WHERE `user_id` = @userId ORDER BY `entry_timestamp` DESC LIMIT 50",
            new { userId });

        var visits = new Dictionary<double, RoomData>();
        foreach (var row in visitsRows)
        {
            if (!_roomFactory.TryGetData((uint)row.room_id, out var data))
                continue;
            if (!visits.ContainsKey((double)row.entry_timestamp))
                visits.Add((double)row.entry_timestamp, data!);
        }

        session.Send(new ModeratorUserRoomVisitsComposer(habbo.Id, habbo.Username, visits));
    }

    public async Task GetUserChatlog(GameClient session, int userId)
    {
        var habbo = _cacheManager.GenerateUser(userId);
        if (habbo == null)
            return;

        using var connection = _database.Connection();
        var visitsRows = await connection.QueryAsync<dynamic>(
            "SELECT `room_id`, `entry_timestamp`, `exit_timestamp` FROM `user_roomvisits` WHERE `user_id` = @userId ORDER BY `entry_timestamp` DESC LIMIT 7",
            new { userId });

        var chatlogs = new List<KeyValuePair<RoomData, List<ChatlogEntry>>>();
        foreach (var row in visitsRows)
        {
            if (!_roomFactory.TryGetData((uint)row.room_id, out var data))
                continue;

            var chats = (await connection.QueryAsync<dynamic>(
                "SELECT `user_id` AS PlayerId, `message` AS Message, `timestamp` AS Timestamp FROM `chatlogs` WHERE `user_id` = @userId AND `room_id` = @roomId AND `timestamp` >= @start AND `timestamp` <= @end ORDER BY `timestamp` DESC",
                new { userId, roomId = row.room_id, start = row.entry_timestamp, end = row.exit_timestamp == 0 ? UnixTimestamp.GetNow() : row.exit_timestamp }))
                .Select(c => new ChatlogEntry((int)c.PlayerId, (uint)row.room_id, (string)c.Message, (double)c.Timestamp)).ToList();

            chatlogs.Add(new(data!, chats));
        }

        session.Send(new ModeratorUserChatlogComposer(habbo.Id, habbo.Username, chatlogs));
    }

    public async Task GetRoomChatlog(GameClient session, uint roomId)
    {
        if (!_roomFactory.TryGetData(roomId, out var data))
            return;

        using var connection = _database.Connection();
        var chats = (await connection.QueryAsync<dynamic>(
            "SELECT `user_id` AS PlayerId, `message` AS Message, `timestamp` AS Timestamp FROM `chatlogs` WHERE `room_id` = @roomId ORDER BY `timestamp` DESC LIMIT 150",
            new { roomId }))
            .Select(c => new ChatlogEntry((int)c.PlayerId, roomId, (string)c.Message, (double)c.Timestamp)).ToList();

        session.Send(new ModeratorRoomChatlogComposer(data!, chats));
    }

    public async Task GetTicketChatlogs(GameClient session, int ticketId)
    {
        if (!_moderationManager.TryGetTicket(ticketId, out var ticket))
            return;

        if (ticket!.Room == null)
            return;

        session.Send(new ModeratorTicketChatlogComposer(ticket, ticket.Room, ticket.Timestamp));
    }
}
