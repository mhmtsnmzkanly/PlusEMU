using Dapper;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Core.Language;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.Utilities;

namespace Plus.HabboHotel.Moderation;

internal class ModerationActionService : IModerationActionService
{
    private readonly IGameClientManager _clientManager;
    private readonly IDatabase _database;
    private readonly IModerationManager _moderationManager;
    private readonly ILanguageManager _languageManager;
    private readonly IRoomService _roomService;

    public ModerationActionService(
        IGameClientManager clientManager,
        IDatabase database,
        IModerationManager moderationManager,
        ILanguageManager languageManager,
        IRoomService roomService)
    {
        _clientManager = clientManager;
        _database = database;
        _moderationManager = moderationManager;
        _languageManager = languageManager;
        _roomService = roomService;
    }

    public async Task SendCaution(GameClient session, int userId, string message)
    {
        var moderator = session.GetHabbo();
        if (!(moderator?.Permissions?.HasRight("mod_caution") ?? false))
            return;

        if (_clientManager.GetClientByUserId(userId) is not GameClient client)
            return;
        var targetHabbo = client.GetHabbo();
        if (targetHabbo == null)
            return;

        using (var connection = _database.Connection())
        {
            await connection.ExecuteAsync(
                "UPDATE `user_info` SET `cautions` = `cautions` + 1 WHERE `user_id` = @userId LIMIT 1",
                new { userId = targetHabbo.Id });
        }

        client.SendNotification(message);
    }

    public Task SendMessage(GameClient session, int userId, string message)
    {
        var moderator = session.GetHabbo();
        if (!(moderator?.Permissions?.HasRight("mod_alert") ?? false))
            return Task.CompletedTask;

        var client = _clientManager.GetClientByUserId(userId);
        if (client == null)
            return Task.CompletedTask;

        client.SendNotification(message);
        return Task.CompletedTask;
    }

    public async Task Mute(GameClient session, int userId, int durationMinutes)
    {
        var moderator = session.GetHabbo();
        if (!(moderator?.Permissions?.HasRight("mod_mute") ?? false))
            return;

        if (_clientManager.GetClientByUserId(userId) is not GameClient client)
            return;
        var targetHabbo = client.GetHabbo();
        if (targetHabbo == null)
        {
            session.SendWhisper("An error occoured whilst finding that user in the database.");
            return;
        }

        if ((targetHabbo.Permissions?.HasRight("mod_mute") ?? false) &&
            !(moderator.Permissions?.HasRight("mod_mute_any") ?? false))
        {
            session.SendWhisper("Oops, you cannot mute that user.");
            return;
        }

        var length = durationMinutes * 60.0;
        using (var connection = _database.Connection())
        {
            await connection.ExecuteAsync(
                "UPDATE `users` SET `time_muted` = @length WHERE `id` = @userId LIMIT 1",
                new { length, userId = targetHabbo.Id });
        }

        targetHabbo.TimeMuted = length;
        client.SendNotification($"You have been muted by a moderator for {length} seconds!");
    }

    public Task Kick(GameClient session, int userId)
    {
        var moderator = session.GetHabbo();
        if (!(moderator?.Permissions?.HasRight("mod_kick") ?? false))
            return Task.CompletedTask;

        if (_clientManager.GetClientByUserId(userId) is not GameClient client)
            return Task.CompletedTask;
        var targetHabbo = client.GetHabbo();
        if (targetHabbo == null || !targetHabbo.TryGetCurrentRoom(out _) || targetHabbo.Id == moderator.Id)
            return Task.CompletedTask;
        if (targetHabbo.Rank >= moderator.Rank)
        {
            session.SendNotification(_languageManager.TryGetValue("moderation.kick.disallowed"));
            return Task.CompletedTask;
        }

        if (!moderator.TryGetCurrentRoom(out _))
            return Task.CompletedTask;

        return _roomService.LeaveRoom(client);
    }

    public async Task Ban(GameClient session, int userId, string message, int durationHours, bool ipBan, bool machineBan)
    {
        var moderator = session.GetHabbo();
        if (!(moderator?.Permissions?.HasRight("mod_soft_ban") ?? false))
            return;

        if (machineBan)
            ipBan = false;

        var client = _clientManager.GetClientByUserId(userId);
        var targetHabbo = client?.GetHabbo();
        
        string targetUsername;
        string targetIp;
        string targetMachine;

        if (targetHabbo == null)
        {
            using (var connection = _database.Connection())
            {
                var targetData = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT `username`, `ip_last`, `machine_id` FROM `users` WHERE `id` = @userId LIMIT 1",
                    new { userId });
                if (targetData == null)
                {
                    session.SendWhisper("An error occurred whilst finding that user in the database.");
                    return;
                }
                targetUsername = targetData.username;
                targetIp = targetData.ip_last;
                targetMachine = targetData.machine_id;
            }
        }
        else
        {
            if ((targetHabbo.Permissions?.HasRight("mod_tool") ?? false) &&
                !(moderator.Permissions?.HasRight("mod_ban_any") ?? false))
            {
                session.SendWhisper("Oops, you cannot ban that user.");
                return;
            }
            targetUsername = targetHabbo.Username;
            targetIp = targetHabbo.MachineId ?? ""; // Wait, machineId is in Habbo. IP is in Socket usually.
            targetMachine = targetHabbo.MachineId ?? "";
            // Let's just fetch from DB to be safe and consistent.
            using (var connection = _database.Connection())
            {
                var targetData = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT `ip_last`, `machine_id` FROM `users` WHERE `id` = @userId LIMIT 1",
                    new { userId = targetHabbo.Id });
                targetIp = targetData?.ip_last ?? "";
                targetMachine = targetData?.machine_id ?? targetMachine;
            }
        }

        var reason = message ?? "No reason specified.";
        var expiresAt = durationHours * 3600 + UnixTimestamp.GetNow();

        using (var connection = _database.Connection())
        {
            await connection.ExecuteAsync(
                "UPDATE `user_info` SET `bans` = `bans` + 1 WHERE `user_id` = @userId LIMIT 1",
                new { userId });
        }

        if (!ipBan && !machineBan)
        {
            await Ban(moderator.Username, ModerationBanType.Username, targetUsername, reason, expiresAt);
        }
        else if (ipBan)
        {
            await Ban(moderator.Username, ModerationBanType.Ip, targetIp, reason, expiresAt);
            await Ban(moderator.Username, ModerationBanType.Username, targetUsername, reason, expiresAt);
        }
        else
        {
            await Ban(moderator.Username, ModerationBanType.Ip, targetIp, reason, expiresAt);
            await Ban(moderator.Username, ModerationBanType.Username, targetUsername, reason, expiresAt);
            await Ban(moderator.Username, ModerationBanType.Machine, targetMachine, reason, expiresAt);
        }

        client?.Disconnect();
    }

    public async Task Ban(string moderatorName, ModerationBanType type, string value, string reason, double expiresAt)
    {
        if (string.IsNullOrEmpty(value)) return;

        var banType = BanTypeUtility.FromModerationBanType(type);
        using (var connection = _database.Connection())
        {
            await connection.ExecuteAsync(
                """
                REPLACE INTO `bans` (`bantype`, `value`, `reason`, `expire`, `added_by`, `added_date`)
                VALUES (@banType, @value, @reason, @expiresAt, @moderatorName, @addedDate)
                """,
                new
                {
                    banType,
                    value,
                    reason,
                    expiresAt,
                    moderatorName,
                    addedDate = UnixTimestamp.GetNow()
                });
        }

        if (type == ModerationBanType.Machine || type == ModerationBanType.Username)
        {
            _moderationManager.AddBan(new(type, value, reason, expiresAt));
        }
    }

    public async Task TradeLock(GameClient session, int userId, string message, int durationMinutes)
    {
        var moderator = session.GetHabbo();
        if (!(moderator?.Permissions?.HasRight("mod_trade_lock") ?? false))
            return;

        var client = _clientManager.GetClientByUserId(userId);
        var targetHabbo = client?.GetHabbo();
        if (targetHabbo == null)
        {
            session.SendWhisper("An error occoured whilst finding that user in the database.");
            return;
        }

        if ((targetHabbo.Permissions?.HasRight("mod_trade_lock") ?? false) &&
            !(moderator.Permissions?.HasRight("mod_trade_lock_any") ?? false))
        {
            session.SendWhisper("Oops, you cannot trade lock another user ranked 5 or higher.");
            return;
        }

        var days = durationMinutes / 1440.0;
        if (days < 1)
            days = 1;
        if (days > 365)
            days = 365;

        var length = UnixTimestamp.GetNow() + days * 86400;
        using (var connection = _database.Connection())
        {
            await connection.ExecuteAsync(
                "UPDATE `user_info` SET `trading_locked` = @length, `trading_locks_count` = `trading_locks_count` + 1 WHERE `user_id` = @userId LIMIT 1",
                new { length, userId = targetHabbo.Id });
        }

        targetHabbo.TradingLockExpiry = length;
        client?.SendNotification($"You have been trade banned for {days} day(s)!\r\rReason:\r\r{message}");
    }

    public Task BroadcastRoomAction(GameClient session, int alertMode, string alertMessage)
    {
        var moderator = session.GetHabbo();
        if (moderator?.Permissions == null || !moderator.Permissions.HasRight("mod_caution"))
            return Task.CompletedTask;
        if (!moderator.TryGetCurrentRoom(out var currentRoom))
            return Task.CompletedTask;

        var isCaution = alertMode != 3;
        var message = isCaution
            ? $"Caution from Moderator:\n\n{alertMessage}"
            : $"Message from Moderator:\n\n{alertMessage}";
        currentRoom.SendPacket(new BroadcastMessageAlertComposer(message));
        return Task.CompletedTask;
    }
}
