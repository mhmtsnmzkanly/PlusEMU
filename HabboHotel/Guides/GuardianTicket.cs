namespace Plus.HabboHotel.Guides;

public sealed class GuardianTicket
{
    public GuardianTicket(int reporterId, int reportedId, IReadOnlyList<string> chatLog, int createdAt)
    {
        ReporterId = reporterId;
        ReportedId = reportedId;
        ChatLog = chatLog;
        CreatedAt = createdAt;
    }

    public int ReporterId { get; }
    public int ReportedId { get; }
    public IReadOnlyList<string> ChatLog { get; }
    public int CreatedAt { get; }
    public int TimeLeftSeconds { get; set; } = 120;
    public GuardianVoteType? Verdict { get; set; }
    public Dictionary<int, GuardianVote> Votes { get; } = new();
}
