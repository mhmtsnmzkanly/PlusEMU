namespace Plus.HabboHotel.Guides;

public sealed class GuardianVote
{
    public GuardianVote(int userId)
    {
        UserId = userId;
        Type = GuardianVoteType.Searching;
    }

    public int UserId { get; }
    public GuardianVoteType Type { get; set; }
    public bool Ignored { get; set; }
}
