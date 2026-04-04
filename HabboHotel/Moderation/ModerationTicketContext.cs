namespace Plus.HabboHotel.Moderation;

public sealed class ModerationTicketContext
{
    public string Type { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int RelatedId { get; init; }
}
