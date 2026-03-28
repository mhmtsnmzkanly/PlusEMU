using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired;

internal static class WiredExecutionAdapter
{
    public static bool ExecuteWithContext(this IWiredItem item, object context)
        => item.Execute(new WiredExecutionContext(context));

    public static bool ExecuteWithActor(this IWiredItem item, Habbo actor)
        => item.Execute(new WiredExecutionContext(actor));

    public static bool ExecuteWithParameters(this IWiredItem item, params object[] parameters)
        => item.Execute(new WiredExecutionContext(parameters));

    public static bool ExecuteWithoutContext(this IWiredItem item)
        => item.Execute(new WiredExecutionContext());
}
