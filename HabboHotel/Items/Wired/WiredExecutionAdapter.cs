using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired;

internal static class WiredExecutionAdapter
{
    public static bool ExecuteWithContext(this IWiredItem item, object context)
    {
        if (item is IWiredExecutable executable)
            return executable.Execute(new(context));

        return item.Execute(context);
    }

    public static bool ExecuteWithActor(this IWiredItem item, Habbo actor)
    {
        if (item is IWiredExecutable executable)
            return executable.Execute(new(actor));

        return item.Execute(actor);
    }

    public static bool ExecuteWithParameters(this IWiredItem item, params object[] parameters)
    {
        if (item is IWiredExecutable executable)
            return executable.Execute(new(parameters));

        return item.Execute(parameters);
    }

    public static bool ExecuteWithoutContext(this IWiredItem item)
    {
        if (item is IWiredExecutable executable)
            return executable.Execute(new());

        return item.Execute();
    }
}
