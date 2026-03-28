using Plus.HabboHotel.Rooms.Instance;

namespace Plus.HabboHotel.Items.Wired;

internal sealed class WiredActorExecutionContext : WiredExecutionContext
{
    public WiredActorExecutionContext(WiredActorTriggerContext context)
    {
        Actor = context.Actor;
    }
}
