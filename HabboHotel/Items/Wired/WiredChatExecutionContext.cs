using Plus.HabboHotel.Rooms.Instance;

namespace Plus.HabboHotel.Items.Wired;

internal sealed class WiredChatExecutionContext : WiredExecutionContext
{
    public WiredChatExecutionContext(WiredChatTriggerContext context)
    {
        Actor = context.Actor;
        Message = context.Message;
    }
}
