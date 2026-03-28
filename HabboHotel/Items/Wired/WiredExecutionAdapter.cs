using Plus.HabboHotel.Users;
using Plus.HabboHotel.Rooms.Instance;

namespace Plus.HabboHotel.Items.Wired;

internal static class WiredExecutionAdapter
{
    public static bool ExecuteWithActor(this IWiredItem item, Habbo actor)
    {
        var executionContext = new WiredActorExecutionContext(new WiredActorTriggerContext(actor));
        if (item is IWiredActorExecutable executable)
            return executable.Execute(executionContext);

        return item.Execute(executionContext);
    }

    public static bool ExecuteWithChat(this IWiredItem item, WiredChatTriggerContext context)
    {
        var executionContext = new WiredChatExecutionContext(context);
        if (item is IWiredChatExecutable executable)
            return executable.Execute(executionContext);

        return item.Execute(executionContext);
    }

    public static bool ExecuteWithActorItem(this IWiredItem item, WiredActorItemTriggerContext context)
    {
        var executionContext = new WiredActorItemExecutionContext(context);
        if (item is IWiredActorItemExecutable executable)
            return executable.Execute(executionContext);

        return item.Execute(executionContext);
    }

    public static bool ExecuteWithoutContext(this IWiredItem item)
    {
        var executionContext = new WiredEmptyExecutionContext();
        if (item is IWiredEmptyExecutable executable)
            return executable.Execute(executionContext);

        return item.Execute(executionContext);
    }
}
