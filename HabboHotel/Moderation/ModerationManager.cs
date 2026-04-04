using System.Collections.Concurrent;
using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;
using Plus.Utilities;

namespace Plus.HabboHotel.Moderation;

public sealed class ModerationManager : IModerationManager
{
    private sealed class ModerationPresetRow
    {
        public string Type { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    private sealed class ModerationTopicRow
    {
        public int Id { get; init; }
        public string Caption { get; init; } = string.Empty;
    }

    private sealed class ModerationTopicActionRow
    {
        public int Id { get; init; }
        public int ParentId { get; init; }
        public string Type { get; init; } = string.Empty;
        public string Caption { get; init; } = string.Empty;
        public string MessageText { get; init; } = string.Empty;
        public int MuteTime { get; init; }
        public int BanTime { get; init; }
        public int IpTime { get; init; }
        public int TradeLockTime { get; init; }
        public string DefaultSanction { get; init; } = string.Empty;
    }

    private sealed class ModerationPresetActionCategoryRow
    {
        public int Id { get; init; }
        public string Caption { get; init; } = string.Empty;
    }

    private sealed class ModerationPresetActionMessageRow
    {
        public int Id { get; init; }
        public int ParentId { get; init; }
        public string Caption { get; init; } = string.Empty;
        public string MessageText { get; init; } = string.Empty;
        public int MuteHours { get; init; }
        public int BanHours { get; init; }
        public int IpBanHours { get; init; }
        public int TradeLockDays { get; init; }
        public string Notice { get; init; } = string.Empty;
    }

    private sealed class ModerationBanRow
    {
        public string BanType { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
        public double Expire { get; init; }
    }

    private readonly IDatabase _database;
    private readonly ILogger<ModerationManager> _logger;
    private readonly Dictionary<string, ModerationBan> _bans = new();
    private readonly Dictionary<int, List<ModerationPresetActions>> _moderationCfhTopicActions = new();


    private readonly Dictionary<int, string> _moderationCfhTopics = new();
    private readonly ConcurrentDictionary<int, ModerationTicket> _modTickets = new();
    private readonly List<string> _roomPresets = new();
    private readonly Dictionary<int, string> _userActionPresetCategories = new();
    private readonly Dictionary<int, List<ModerationPresetActionMessages>> _userActionPresetMessages = new();
    private readonly List<string> _userPresets = new();

    private int _ticketCount = 1;

    public ICollection<string> UserMessagePresets => _userPresets;

    public ICollection<string> RoomMessagePresets => _roomPresets;

    public ICollection<ModerationTicket> GetTickets => _modTickets.Values;

    public ModerationManager(IDatabase database, ILogger<ModerationManager> logger)
    {
        _database = database;
        _logger = logger;
    }

    public Dictionary<string, List<ModerationPresetActions>> UserActionPresets
    {
        get
        {
            var result = new Dictionary<string, List<ModerationPresetActions>>();
            foreach (var category in _moderationCfhTopics.ToList())
            {
                result.Add(category.Value, new());
                if (_moderationCfhTopicActions.ContainsKey(category.Key))
                    foreach (var data in _moderationCfhTopicActions[category.Key])
                        result[category.Value].Add(data);
            }
            return result;
        }
    }

    public void Init()
    {
        if (_userPresets.Count > 0)
            _userPresets.Clear();
        if (_moderationCfhTopics.Count > 0)
            _moderationCfhTopics.Clear();
        if (_moderationCfhTopicActions.Count > 0)
            _moderationCfhTopicActions.Clear();
        if (_userActionPresetCategories.Count > 0)
            _userActionPresetCategories.Clear();
        if (_userActionPresetMessages.Count > 0)
            _userActionPresetMessages.Clear();
        if (_roomPresets.Count > 0)
            _roomPresets.Clear();
        if (_bans.Count > 0)
            _bans.Clear();
        using (var connection = _database.Connection())
        {
            foreach (var row in connection.Query<ModerationPresetRow>(
                         "SELECT `type` AS Type, `message` AS Message FROM `moderation_presets`"))
            {
                switch (row.Type.ToLower())
                {
                    case "user":
                        _userPresets.Add(row.Message);
                        break;
                    case "room":
                        _roomPresets.Add(row.Message);
                        break;
                }
            }

            foreach (var row in connection.Query<ModerationTopicRow>(
                         "SELECT `id` AS Id, `caption` AS Caption FROM `moderation_topics`"))
            {
                if (!_moderationCfhTopics.ContainsKey(row.Id))
                    _moderationCfhTopics.Add(row.Id, row.Caption);
            }

            foreach (var row in connection.Query<ModerationTopicActionRow>(
                         """
                         SELECT
                             `id` AS Id,
                             `parent_id` AS ParentId,
                             `type` AS Type,
                             `caption` AS Caption,
                             `message_text` AS MessageText,
                             `mute_time` AS MuteTime,
                             `ban_time` AS BanTime,
                             `ip_time` AS IpTime,
                             `trade_lock_time` AS TradeLockTime,
                             `default_sanction` AS DefaultSanction
                         FROM `moderation_topic_actions`
                         """))
            {
                if (!_moderationCfhTopicActions.ContainsKey(row.ParentId)) _moderationCfhTopicActions.Add(row.ParentId, new());
                _moderationCfhTopicActions[row.ParentId].Add(new(row.Id, row.ParentId, row.Type, row.Caption, row.MessageText,
                    row.MuteTime, row.BanTime, row.IpTime, row.TradeLockTime, row.DefaultSanction));
            }

            foreach (var row in connection.Query<ModerationPresetActionCategoryRow>(
                         "SELECT `id` AS Id, `caption` AS Caption FROM `moderation_preset_action_categories`"))
            {
                _userActionPresetCategories[row.Id] = row.Caption;
            }

            foreach (var row in connection.Query<ModerationPresetActionMessageRow>(
                         """
                         SELECT
                             `id` AS Id,
                             `parent_id` AS ParentId,
                             `caption` AS Caption,
                             `message_text` AS MessageText,
                             `mute_hours` AS MuteHours,
                             `ban_hours` AS BanHours,
                             `ip_ban_hours` AS IpBanHours,
                             `trade_lock_days` AS TradeLockDays,
                             `notice` AS Notice
                         FROM `moderation_preset_action_messages`
                         """))
            {
                if (!_userActionPresetMessages.ContainsKey(row.ParentId)) _userActionPresetMessages.Add(row.ParentId, new());
                _userActionPresetMessages[row.ParentId].Add(new(row.Id, row.ParentId, row.Caption, row.MessageText,
                    row.MuteHours, row.BanHours, row.IpBanHours, row.TradeLockDays, row.Notice));
            }

            RebuildBanCache(connection);
        }
        _logger.LogInformation("Loaded " + (_userPresets.Count + _roomPresets.Count) + " moderation presets.");
        _logger.LogInformation("Loaded " + _userActionPresetCategories.Count + " moderation categories.");
        _logger.LogInformation("Loaded " + _userActionPresetMessages.Count + " moderation action preset messages.");
        _logger.LogInformation("Cached " + _bans.Count + " username and machine bans.");
    }

    public void ReCacheBans()
    {
        if (_bans.Count > 0)
            _bans.Clear();
        using (var connection = _database.Connection())
            RebuildBanCache(connection);
        _logger.LogInformation("Cached " + _bans.Count + " username and machine bans.");
    }

    public void AddBan(ModerationBan ban)
    {
        if (ban.Type == ModerationBanType.Machine || ban.Type == ModerationBanType.Username)
        {
            if (!_bans.ContainsKey(ban.Value))
                _bans.Add(ban.Value, ban);
        }
    }


    public bool TryAddTicket(ModerationTicket ticket)
    {
        ticket.Id = _ticketCount++;
        return _modTickets.TryAdd(ticket.Id, ticket);
    }

    public bool TryGetTicket(int ticketId, out ModerationTicket? ticket) => _modTickets.TryGetValue(ticketId, out ticket);

    public bool TryGetTopicAction(int topicId, out ModerationPresetActions? action)
    {
        action = _moderationCfhTopicActions.Values
            .SelectMany(actions => actions)
            .FirstOrDefault(entry => entry.Id == topicId);
        return action != null;
    }

    public bool TryGetTopicCaption(int topicId, out string? caption)
    {
        caption = _moderationCfhTopicActions.Values
            .SelectMany(actions => actions)
            .Where(entry => entry.Id == topicId)
            .Select(entry => entry.Caption)
            .FirstOrDefault();
        return !string.IsNullOrWhiteSpace(caption);
    }

    public bool UserHasTickets(int userId) => _modTickets.Any(x => x.Value.Sender.Id == userId && x.Value.Answered == false);

    public bool TryGetTicketBySenderId(int userId, out ModerationTicket? ticket)
    {
        ticket = _modTickets.Values.FirstOrDefault(entry => entry.Sender.Id == userId);
        return ticket != null;
    }

    /// <summary>
    /// Runs a quick check to see if a ban record is cached in the server.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ban"></param>
    /// <returns></returns>
    public bool IsBanned(string key, out ModerationBan? ban)
    {
        if (_bans.TryGetValue(key, out ban))
        {
            if (!ban.Expired)
                return true;

            //This ban has expired, let us quickly remove it here.
            using (var connection = _database.Connection())
                connection.Execute(
                    "DELETE FROM `bans` WHERE `bantype` = @banType AND `value` = @key LIMIT 1",
                    new { banType = BanTypeUtility.FromModerationBanType(ban.Type), key });

            //And finally, let us remove the ban record from the cache.
            _bans.Remove(key);
            return false;
        }
        return false;
    }

    /// <summary>
    /// Run a quick database check to see if this ban exists in the database.
    /// </summary>
    /// <param name="machineId">The value of the ban.</param>
    /// <returns></returns>
    public bool HasMachineBanCheck(string machineId)
    {
        if (IsBanned(machineId, out var machineBanRecord))
        {
            using var connection = _database.Connection();
            var banExists = connection.ExecuteScalar<int>(
                "SELECT 1 FROM `bans` WHERE `bantype` = 'machine' AND `value` = @value LIMIT 1",
                new { value = machineId });

            //If there is no more ban record, then we can simply remove it from our cache!
            if (banExists == 0)
            {
                RemoveBan(machineId);
                return false;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Run a quick database check to see if this ban exists in the database.
    /// </summary>
    /// <param name="username">The value of the ban.</param>
    /// <returns></returns>
    public bool UsernameBanCheck(string username)
    {
        if (IsBanned(username, out var usernameBanRecord))
        {
            using var connection = _database.Connection();
            var banExists = connection.ExecuteScalar<int>(
                "SELECT 1 FROM `bans` WHERE `bantype` = 'user' AND `value` = @value LIMIT 1",
                new { value = username });

            //If there is no more ban record, then we can simply remove it from our cache!
            if (banExists == 0)
            {
                RemoveBan(username);
                return false;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Remove a ban from the cache based on a given value.
    /// </summary>
    /// <param name="value"></param>
    public void RemoveBan(string value)
    {
        _bans.Remove(value);
    }

    private void RebuildBanCache(System.Data.IDbConnection connection)
    {
        foreach (var row in connection.Query<ModerationBanRow>(
                     "SELECT `bantype` AS BanType, `value` AS Value, `reason` AS Reason, `expire` AS Expire FROM `bans` WHERE `bantype` = 'machine' OR `bantype` = 'user'"))
        {
            var ban = new ModerationBan(BanTypeUtility.GetModerationBanType(row.BanType), row.Value, row.Reason, row.Expire);
            if (row.Expire > UnixTimestamp.GetNow())
            {
                if (!_bans.ContainsKey(row.Value))
                    _bans.Add(row.Value, ban);
            }
            else
            {
                connection.Execute(
                    "DELETE FROM `bans` WHERE `bantype` = @banType AND `value` = @key LIMIT 1",
                    new { banType = BanTypeUtility.FromModerationBanType(ban.Type), key = row.Value });
            }
        }
    }
}
