namespace Plus.HabboHotel.Moderation;

public sealed class ModerationTicketChatEntry
{
    public ModerationTicketChatEntry(int entryId, string message)
    {
        EntryId = entryId;
        Message = message ?? string.Empty;
    }

    public int EntryId { get; }
    public string Message { get; }
}
