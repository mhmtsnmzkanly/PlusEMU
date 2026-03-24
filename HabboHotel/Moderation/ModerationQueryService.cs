using System.Data;
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

        using var dbClient = _database.GetQueryReactor();
        dbClient.SetQuery("SELECT `id`,`username`,`online`,`mail`,`ip_last`,`look`,`account_created`,`last_online` FROM `users` WHERE `id` = @userId LIMIT 1");
        dbClient.AddParameter("userId", userId);
        var user = dbClient.GetRow();
        if (user == null)
        {
            session.SendNotification(_languageManager.TryGetValue("user.not_found"));
            return Task.CompletedTask;
        }

        dbClient.SetQuery("SELECT `cfhs`,`cfhs_abusive`,`cautions`,`bans`,`trading_locked`,`trading_locks_count` FROM `user_info` WHERE `user_id` = @userId LIMIT 1");
        dbClient.AddParameter("userId", userId);
        var info = dbClient.GetRow();
        if (info == null)
        {
            dbClient.RunQuery($"INSERT INTO `user_info` (`user_id`) VALUES ('{userId}')");
            dbClient.SetQuery("SELECT `cfhs`,`cfhs_abusive`,`cautions`,`bans`,`trading_locked`,`trading_locks_count` FROM `user_info` WHERE `user_id` = @userId LIMIT 1");
            dbClient.AddParameter("userId", userId);
            info = dbClient.GetRow();
        }

        if (info != null)
            session.Send(new ModeratorUserInfoComposer(user, info));

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
        using var dbClient = _database.GetQueryReactor();
        dbClient.SetQuery("SELECT `room_id`, `entry_timestamp` FROM `user_roomvisits` WHERE `user_id` = @id ORDER BY `entry_timestamp` DESC LIMIT 50");
        dbClient.AddParameter("id", userId);
        var table = dbClient.GetTable();
        if (table != null)
        {
            foreach (DataRow row in table.Rows)
            {
                if (!RoomFactory.TryGetData(Convert.ToUInt32(row["room_id"]), out var data))
                    continue;
                var timestamp = Convert.ToDouble(row["entry_timestamp"]);
                if (!visits.ContainsKey(timestamp))
                    visits.Add(timestamp, data);
            }
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
        using var dbClient = _database.GetQueryReactor();
        dbClient.SetQuery($"SELECT `room_id`,`entry_timestamp`,`exit_timestamp` FROM `user_roomvisits` WHERE `user_id` = '{data.Id}' ORDER BY `entry_timestamp` DESC LIMIT 7");
        var getLogs = dbClient.GetTable();
        if (getLogs != null)
        {
            foreach (DataRow row in getLogs.Rows)
            {
                if (!RoomFactory.TryGetData(Convert.ToUInt32(row["room_id"]), out var roomData))
                    continue;
                var timestampExit = Convert.ToDouble(row["exit_timestamp"]) <= 0 ? UnixTimestamp.GetNow() : Convert.ToDouble(row["exit_timestamp"]);
                chatlogs.Add(new(roomData, GetChatlogs(roomData, Convert.ToDouble(row["entry_timestamp"]), timestampExit)));
            }
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
        using var dbClient = _database.GetQueryReactor();
        dbClient.SetQuery("SELECT * FROM `chatlogs` WHERE `room_id` = @id ORDER BY `id` DESC LIMIT 100");
        dbClient.AddParameter("id", roomId);
        var data = dbClient.GetTable();
        if (data != null)
        {
            foreach (DataRow row in data.Rows)
            {
                var chatHabbo = _clientManager.GetClientByUserId(Convert.ToInt32(row["user_id"]))?.GetHabbo();
                if (chatHabbo != null)
                    chats.Add(new(Convert.ToInt32(row["user_id"]), roomId, Convert.ToString(row["message"]) ?? string.Empty, Convert.ToDouble(row["timestamp"]), chatHabbo));
            }
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
        using var dbClient = _database.GetQueryReactor();
        dbClient.SetQuery(
            $"SELECT `user_id`, `timestamp`, `message` FROM `chatlogs` WHERE `room_id` = {roomData.Id} AND `timestamp` > {timeEnter} AND `timestamp` < {timeExit} ORDER BY `timestamp` DESC LIMIT 100");
        var data = dbClient.GetTable();
        if (data != null)
        {
            foreach (DataRow row in data.Rows)
            {
                var habbo = _clientManager.GetClientByUserId(Convert.ToInt32(row["user_id"]))?.GetHabbo();
                if (habbo != null)
                    chats.Add(new(Convert.ToInt32(row["user_id"]), roomData.Id, Convert.ToString(row["message"]) ?? string.Empty, Convert.ToDouble(row["timestamp"]), habbo));
            }
        }

        return chats;
    }
}
