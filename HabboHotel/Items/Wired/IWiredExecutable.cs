namespace Plus.HabboHotel.Items.Wired;

public interface IWiredExecutable
{
    bool Execute(WiredExecutionContext context);
}

internal interface IWiredChatExecutable
{
    bool Execute(WiredChatExecutionContext context);
}

internal interface IWiredActorItemExecutable
{
    bool Execute(WiredActorItemExecutionContext context);
}

internal interface IWiredEmptyExecutable
{
    bool Execute(WiredEmptyExecutionContext context);
}
