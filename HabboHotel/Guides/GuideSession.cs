namespace Plus.HabboHotel.Guides;

public sealed class GuideSession
{
    public GuideSession(int requesterId, int requestType, string message, int createdAt)
    {
        RequesterId = requesterId;
        RequestType = requestType;
        Message = message;
        CreatedAt = createdAt;
    }

    public int RequesterId { get; }
    public int RequestType { get; }
    public string Message { get; }
    public int CreatedAt { get; }
    public int? HelperId { get; set; }
    public bool Started { get; set; }
    public HashSet<int> DeclinedHelperIds { get; } = new();
    public List<GuideChatMessage> Messages { get; } = new();
}
