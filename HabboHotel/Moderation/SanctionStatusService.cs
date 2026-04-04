using Dapper;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.Utilities;

namespace Plus.HabboHotel.Moderation;

internal sealed class SanctionStatusService : ISanctionStatusService
{
    private sealed class SanctionStatusRow
    {
        public string Username { get; init; } = string.Empty;
        public string MachineId { get; init; } = string.Empty;
        public double TimeMuted { get; init; }
        public double TradingLocked { get; init; }
        public int TradingLocksCount { get; init; }
        public int Cautions { get; init; }
        public int Bans { get; init; }
    }

    private readonly IDatabase _database;
    private readonly IModerationManager _moderationManager;

    public SanctionStatusService(IDatabase database, IModerationManager moderationManager)
    {
        _database = database;
        _moderationManager = moderationManager;
    }

    public async Task<SanctionStatusData> GetStatus(GameClient session)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return CreateEmptyStatus();

        using var connection = _database.Connection();
        var row = await connection.QuerySingleOrDefaultAsync<SanctionStatusRow>(
            """
            SELECT
                u.`username` AS Username,
                COALESCE(u.`machine_id`, '') AS MachineId,
                COALESCE(u.`time_muted`, 0) AS TimeMuted,
                COALESCE(ui.`trading_locked`, 0) AS TradingLocked,
                COALESCE(ui.`trading_locks_count`, 0) AS TradingLocksCount,
                COALESCE(ui.`cautions`, 0) AS Cautions,
                COALESCE(ui.`bans`, 0) AS Bans
            FROM `users` u
            LEFT JOIN `user_info` ui ON ui.`user_id` = u.`id`
            WHERE u.`id` = @userId
            LIMIT 1
            """,
            new { userId = habbo.Id });

        if (row == null)
            return CreateEmptyStatus();

        var activeBan = TryGetActiveBan(row.Username, row.MachineId, out var ban) ? ban : null;
        var now = UnixTimestamp.GetNow();
        var currentText = GetCurrentSanctionText(row, activeBan, now, out var currentHours, out var isMuted, out var usesCustomMessage);

        return new SanctionStatusData
        {
            HasCurrentSanction = activeBan != null || row.TimeMuted > 0,
            UsesCustomMessage = usesCustomMessage,
            CurrentSanctionText = currentText,
            CurrentSanctionHours = currentHours,
            ProbationDaysLeft = 0,
            NextSanctionText = GetNextSanctionText(row),
            InfoTitle = "Your sanction information.",
            CautionCount = row.Cautions,
            Disclaimer = string.Empty,
            BanCount = row.Bans,
            TradeLockCount = row.TradingLocksCount,
            IsMuted = isMuted
        };
    }

    private bool TryGetActiveBan(string username, string machineId, out ModerationBan? ban)
    {
        if (_moderationManager.IsBanned(username, out ban))
            return true;

        if (!string.IsNullOrWhiteSpace(machineId) && _moderationManager.IsBanned(machineId, out ban))
            return true;

        ban = null;
        return false;
    }

    private static string GetCurrentSanctionText(SanctionStatusRow row, ModerationBan? ban, double now, out int currentHours, out bool isMuted, out bool usesCustomMessage)
    {
        currentHours = 0;
        isMuted = false;
        usesCustomMessage = false;

        if (ban != null)
        {
            currentHours = GetRoundedHours(Math.Max(0, ban.Expire - now));
            return currentHours >= 24
                ? $"You were banned for {Math.Max(1, (int)Math.Ceiling(currentHours / 24.0))} days."
                : $"You were banned for {Math.Max(1, currentHours)} hour(s).";
        }

        if (row.TimeMuted > 0)
        {
            currentHours = GetRoundedHours(row.TimeMuted);
            isMuted = true;
            usesCustomMessage = true;
            return currentHours > 0
                ? $"You were muted for {currentHours} hour(s)."
                : "You have been muted temporarily.";
        }

        if (row.TradingLocked > now)
        {
            currentHours = GetRoundedHours(row.TradingLocked - now);
            return currentHours >= 24
                ? $"Your trading privileges are restricted for {Math.Max(1, (int)Math.Ceiling(currentHours / 24.0))} days."
                : $"Your trading privileges are restricted for {Math.Max(1, currentHours)} hour(s).";
        }

        if (row.Cautions > 0)
            return "You got alerted for your actions.";

        return "Your sanction record is as clean as Frank's rubber duckie. Keep up the good work!";
    }

    private static string GetNextSanctionText(SanctionStatusRow row)
    {
        var severity = row.Cautions + row.Bans + row.TradingLocksCount;
        return severity switch
        {
            <= 0 => "Next sanction: Alert. Remember to play nice!",
            1 => "Next sanction: 2h mute. Frank is worried.",
            2 => "Next sanction: 18h ban. Behave!",
            _ => "Next sanction: a veeeery long ban. Like forever."
        };
    }

    private static int GetRoundedHours(double seconds)
    {
        if (seconds <= 0)
            return 0;

        return Math.Max(1, (int)Math.Ceiling(seconds / 3600.0));
    }

    private static SanctionStatusData CreateEmptyStatus()
    {
        return new SanctionStatusData
        {
            HasCurrentSanction = false,
            UsesCustomMessage = false,
            CurrentSanctionText = "Your sanction record is as clean as Frank's rubber duckie. Keep up the good work!",
            CurrentSanctionHours = 0,
            ProbationDaysLeft = 0,
            NextSanctionText = "Next sanction: Alert. Remember to play nice!",
            InfoTitle = "Your sanction information.",
            CautionCount = 0,
            Disclaimer = string.Empty,
            BanCount = 0,
            TradeLockCount = 0,
            IsMuted = false
        };
    }
}
