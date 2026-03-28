namespace Plus.HabboHotel.Items.Wired;

public interface IWiredExecutable
{
    bool Execute(WiredExecutionContext context);
}

internal interface IWiredChatExecutable : IWiredExecutable
{
    bool Execute(WiredChatExecutionContext context);
    bool IWiredExecutable.Execute(WiredExecutionContext context) => Execute((WiredChatExecutionContext)context);
}

internal interface IWiredActorItemExecutable : IWiredExecutable
{
    bool Execute(WiredActorItemExecutionContext context);
    bool IWiredExecutable.Execute(WiredExecutionContext context) => Execute((WiredActorItemExecutionContext)context);
}

internal interface IWiredActorExecutable : IWiredExecutable
{
    bool Execute(WiredActorExecutionContext context);
    bool IWiredExecutable.Execute(WiredExecutionContext context) => Execute((WiredActorExecutionContext)context);
}

internal interface IWiredEmptyExecutable : IWiredExecutable
{
    bool Execute(WiredEmptyExecutionContext context);
    bool IWiredExecutable.Execute(WiredExecutionContext context) => Execute((WiredEmptyExecutionContext)context);
}
