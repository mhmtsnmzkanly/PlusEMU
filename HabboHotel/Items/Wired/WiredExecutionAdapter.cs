using Plus.HabboHotel.Users;
using Plus.HabboHotel.Rooms.Instance;

namespace Plus.HabboHotel.Items.Wired;

internal static class WiredExecutionAdapter
{
    public static bool ExecuteWithContext(this IWiredItem item, object context)
        => item.Execute(new WiredExecutionContext(context));

    public static bool ExecuteWithActor(this IWiredItem item, Habbo actor)
        => item.Execute(new WiredActorExecutionContext(new WiredActorTriggerContext(actor)));

    public static bool ExecuteWithParameters(this IWiredItem item, params object[] parameters)
        => item.Execute(new WiredExecutionContext(parameters));

    public static bool ExecuteWithChat(this IWiredItem item, WiredChatTriggerContext context)
        => item.Execute(new WiredChatExecutionContext(context));

    public static bool ExecuteWithActorItem(this IWiredItem item, WiredActorItemTriggerContext context)
        => item.Execute(new WiredActorItemExecutionContext(context));

    public static bool ExecuteWithoutContext(this IWiredItem item)
        => item.Execute(new WiredExecutionContext());
}
