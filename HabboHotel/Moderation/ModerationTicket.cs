using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Moderation;

public class ModerationTicket
{
    public List<ModerationTicketChatEntry> ReportedChats;

    public ModerationTicket(int id, int type, int category, double timestamp, int priority, Habbo sender, Habbo? reported, int reportedUserId, string? reportedUsername, string issue, RoomData? room, string? contextType, string? contextLabel, List<ModerationTicketChatEntry> reportedChats)
    {
        Id = id;
        Type = type;
        Category = category;
        Timestamp = timestamp;
        Priority = priority;
        Sender = sender;
        Reported = reported;
        ReportedUserId = reported?.Id ?? reportedUserId;
        ReportedUsername = reported?.Username ?? reportedUsername ?? string.Empty;
        Moderator = null;
        Issue = issue;
        Room = room;
        ContextType = contextType ?? string.Empty;
        ContextLabel = contextLabel ?? string.Empty;
        Answered = false;
        ReportedChats = reportedChats;
    }

    public int Id { get; set; }
    public int Type { get; set; }
    public int Category { get; set; }
    public double Timestamp { get; set; }
    public int Priority { get; set; }
    public bool Answered { get; set; }
    public Habbo Sender { get; set; }
    public Habbo? Reported { get; set; }
    public int ReportedUserId { get; set; }
    public string ReportedUsername { get; set; }
    public Habbo? Moderator { get; set; }
    public string Issue { get; set; }
    public RoomData? Room { get; set; }
    public string ContextType { get; set; }
    public string ContextLabel { get; set; }

    public int GetStatus(int id)
    {
        if (Moderator == null)
            return 1;
        if (Moderator.Id == id && !Answered)
            return 2;
        if (Answered)
            return 3;
        return 3;
    }
}
