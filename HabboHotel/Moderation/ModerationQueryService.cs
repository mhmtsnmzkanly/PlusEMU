using Dapper;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Core.Language;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat.Logs;
using Plus.HabboHotel.Users;
using Plus.Utilities;

namespace Plus.HabboHotel.Moderation;

internal class ModerationQueryService : IModerationQueryService
{
    private sealed class ModeratorUserInfoRow
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public int Online { get; set; }
        public string Mail { get; set; } = string.Empty;
        public string IpLast { get; set; } = string.Empty;
        public string Look { get; set; } = string.Empty;
        public double AccountCreated { get; set; }
        public double LastOnline { get; set; }
    }

    private sealed class ModeratorUserStatsRow
    {
        public int Cfhs { get; set; }
        public int CfhsAbusive { get; set; }
        public int Cautions { get; set; }
        public int Bans { get; set; }
        public double TradingLocked { get; set; }
        public int TradingLocksCount { get; set; }
    }

    private sealed class RoomVisitRow
    {
        public uint RoomId { get; set; }
        public double EntryTimestamp { get; set; }
        public double ExitTimestamp { get; set; }
    }

    private sealed class ChatlogRow
    {
        public int UserId { get; set; }
        public double Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private readonly ILanguageManager _languageManager;
    private readonly IDatabase _database;
    private readonly IRoomManager _roomManager;
    private readonly IChatlogManager _chatlogManager;
    private readonly IGameClientManager _clientManager;
    private readonly IModerationManager _moderationManager;

    public ModerationQueryService(
        ILanguageManager languageManager,
        IDatabase database,
        IRoomManager roomManager,
        IChatlogManager chatlogManager,
        IGameClientManager clientManager,
        IModerationManager moderationManager)
    {
        _languageManager = languageManager;
        _database = database;
        _roomManager = roomManager;
        _chatlogManager = chatlogManager;
        _clientManager = clientManager;
        _moderationManager = moderationManager;
    }

    public Task GetUserInfo(GameClient session, int userId)
    {
        var habbo = session.GetHabbo();
        if (!(habbo?.Permissions?.HasRight("mod_tool") ?? false))
            return Task.CompletedTask;

        using var connection = _database.Connection();
        var user = connection.QueryFirstOrDefault<ModeratorUserInfoRow>(
            """
            SELECT
                `id` AS Id,
                `username` AS Username,
                `online` AS Online,
                `mail` AS Mail,
                `ip_last` AS IpLast,
                `look` AS Look,
                `account_created` AS AccountCreated,
                `last_online` AS LastOnline
            FROM `users`
            WHERE `id` = @userId
            LIMIT 1
            """,
            new { userId });
        if (user == null)
        {
            session.SendNotification(_languageManager.TryGetValue("user.not_found"));
            return Task.CompletedTask;
        }

        var info = connection.QueryFirstOrDefault<ModeratorUserStatsRow>(
            """
            SELECT
                `cfhs` AS Cfhs,
                `cfhs_abusive` AS CfhsAbusive,
                `cautions` AS Cautions,
                `bans` AS Bans,
                `trading_locked` AS TradingLocked,
                `trading_locks_count` AS TradingLocksCount
            FROM `user_info`
            WHERE `user_id` = @userId
            LIMIT 1
            """,
            new { userId });
        if (info == null)
        {
            connection.Execute(
                "INSERT INTO `user_info` (`user_id`) VALUES (@userId)",
                new { userId });
            info = connection.QueryFirstOrDefault<ModeratorUserStatsRow>(
                """
                SELECT
                    `cfhs` AS Cfhs,
                    `cfhs_abusive` AS CfhsAbusive,
                    `cautions` AS Cautions,
                    `bans` AS Bans,
                    `trading_locked` AS TradingLocked,
                    `trading_locks_count` AS TradingLocksCount
                FROM `user_info`
                WHERE `user_id` = @userId
                LIMIT 1
                """,
                new { userId });
        }

        if (info != null)
            session.Send(new ModeratorUserInfoComposer(
                user,
                info,
                _clientManager.GetClientByUserId(user.Id) != null));

        return Task.CompletedTask;
    }

    public Task GetRoomInfo(GameClient session, uint roomId)
    {
        var habbo = session.GetHabbo();
        if (!(habbo?.Permissions?.HasRight("mod_tool") ?? false))
            return Task.CompletedTask;

        if (!RoomFactory.TryGetData(roomId, out var data))
            return Task.CompletedTask;
        if (!_roomManager.TryGetRoom(roomId, out var room))
            return Task.CompletedTask;

        session.Send(new ModeratorRoomInfoComposer(data, room.GetRoomUserManager().GetRoomUserByHabbo(data.OwnerName) != null));
        return Task.CompletedTask;
    }

    public Task GetUserRoomVisits(GameClient session, int userId)
    {
        var habbo = session.GetHabbo();
        if (!(habbo?.Permissions?.HasRight("mod_tool") ?? false))
            return Task.CompletedTask;

        var target = _clientManager.GetClientByUserId(userId);
        var targetHabbo = target?.GetHabbo();
        if (targetHabbo == null)
            return Task.CompletedTask;

        var visits = new Dictionary<double, RoomData>();
        using var connection = _database.Connection();
        var rows = connection.Query<RoomVisitRow>(
            "SELECT `room_id` AS RoomId, `entry_timestamp` AS EntryTimestamp FROM `user_roomvisits` WHERE `user_id` = @id ORDER BY `entry_timestamp` DESC LIMIT 50",
            new { id = userId });
        foreach (var row in rows)
        {
            if (!RoomFactory.TryGetData(row.RoomId, out var data))
                continue;
            if (!visits.ContainsKey(row.EntryTimestamp))
                visits.Add(row.EntryTimestamp, data);
        }

        session.Send(new ModeratorUserRoomVisitsComposer(targetHabbo, visits));
        return Task.CompletedTask;
    }

    public Task GetUserChatlog(GameClient session, int userId)
    {
        var habbo = session.GetHabbo();
        if (!(habbo?.Permissions?.HasRight("mod_tool") ?? false))
            return Task.CompletedTask;

        var data = _clientManager.GetClientByUserId(userId)?.GetHabbo();
        if (data == null)
        {
            session.SendNotification("Unable to load info for user.");
            return Task.CompletedTask;
        }

        _chatlogManager.FlushAndSave();
        var chatlogs = new List<KeyValuePair<RoomData, List<ChatlogEntry>>>();
        using var connection = _database.Connection();
        var visits = connection.Query<RoomVisitRow>(
            "SELECT `room_id` AS RoomId, `entry_timestamp` AS EntryTimestamp, `exit_timestamp` AS ExitTimestamp FROM `user_roomvisits` WHERE `user_id` = @userId ORDER BY `entry_timestamp` DESC LIMIT 7",
            new { userId = data.Id });
        foreach (var row in visits)
        {
            if (!RoomFactory.TryGetData(row.RoomId, out var roomData))
                continue;
            var timestampExit = row.ExitTimestamp <= 0 ? UnixTimestamp.GetNow() : row.ExitTimestamp;
            chatlogs.Add(new(roomData, GetChatlogs(roomData, row.EntryTimestamp, timestampExit)));
        }

        session.Send(new ModeratorUserChatlogComposer(data, chatlogs));
        return Task.CompletedTask;
    }

    public Task GetRoomChatlog(GameClient session, uint roomId)
    {
        var habbo = session.GetHabbo();
        if (!(habbo?.Permissions?.HasRight("mod_tool") ?? false))
            return Task.CompletedTask;

        if (!_roomManager.TryGetRoom(roomId, out var room))
            return Task.CompletedTask;

        _chatlogManager.FlushAndSave();
        var chats = new List<ChatlogEntry>();
        using var connection = _database.Connection();
        var rows = connection.Query<ChatlogRow>(
            "SELECT `user_id` AS UserId, `timestamp` AS Timestamp, `message` AS Message FROM `chatlogs` WHERE `room_id` = @id ORDER BY `id` DESC LIMIT 100",
            new { id = roomId });
        foreach (var row in rows)
        {
            var chatHabbo = _clientManager.GetClientByUserId(row.UserId)?.GetHabbo();
            if (chatHabbo != null)
                chats.Add(new(row.UserId, roomId, row.Message ?? string.Empty, row.Timestamp, chatHabbo));
        }

        session.Send(new ModeratorRoomChatlogComposer(room, chats));
        return Task.CompletedTask;
    }

    public Task GetTicketChatlogs(GameClient session, int ticketId)
    {
        var habbo = session.GetHabbo();
        if (!(habbo?.Permissions?.HasRight("mod_tickets") ?? false))
            return Task.CompletedTask;

        if (!_moderationManager.TryGetTicket(ticketId, out var ticket) || ticket?.Room == null)
            return Task.CompletedTask;
        if (!RoomFactory.TryGetData(ticket.Room.Id, out var data))
            return Task.CompletedTask;

        session.Send(new ModeratorTicketChatlogComposer(ticket, data, ticket.Timestamp));
        return Task.CompletedTask;
    }

    private List<ChatlogEntry> GetChatlogs(RoomData roomData, double timeEnter, double timeExit)
    {
        var chats = new List<ChatlogEntry>();
        using var connection = _database.Connection();
        var rows = connection.Query<ChatlogRow>(
            "SELECT `user_id` AS UserId, `timestamp` AS Timestamp, `message` AS Message FROM `chatlogs` WHERE `room_id` = @roomId AND `timestamp` > @timeEnter AND `timestamp` < @timeExit ORDER BY `timestamp` DESC LIMIT 100",
            new { roomId = roomData.Id, timeEnter, timeExit });
        foreach (var row in rows)
        {
            var habbo = _clientManager.GetClientByUserId(row.UserId)?.GetHabbo();
            if (habbo != null)
                chats.Add(new(row.UserId, roomData.Id, row.Message ?? string.Empty, row.Timestamp, habbo));
        }

        return chats;
    }
}
