namespace Plus.HabboHotel.Moderation;

public sealed class SanctionStatusData
{
    public bool HasCurrentSanction { get; init; }
    public bool UsesCustomMessage { get; init; }
    public string CurrentSanctionText { get; init; } = string.Empty;
    public int CurrentSanctionHours { get; init; }
    public int ProbationDaysLeft { get; init; }
    public string NextSanctionText { get; init; } = string.Empty;
    public string InfoTitle { get; init; } = string.Empty;
    public int CautionCount { get; init; }
    public string Disclaimer { get; init; } = string.Empty;
    public int BanCount { get; init; }
    public int TradeLockCount { get; init; }
    public bool IsMuted { get; init; }
}
