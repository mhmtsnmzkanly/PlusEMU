using Plus.HabboHotel.Rooms.Instance;

namespace Plus.HabboHotel.Items.Wired;

internal sealed class WiredActorItemExecutionContext : WiredExecutionContext
{
    public WiredActorItemExecutionContext(WiredActorItemTriggerContext context)
    {
        Parameters = [context];
        Actor = context.Actor;
        Item = context.Item;
    }
}
