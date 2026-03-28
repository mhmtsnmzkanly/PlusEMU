using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.Chat.Commands;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired;

internal sealed class WiredExecutionContext
{
    public WiredExecutionContext(params object[] parameters)
    {
        Parameters = parameters;
        if (WiredContextResolver.TryGetActor(parameters, out var actor))
            Actor = actor;
        if (WiredContextResolver.TryGetActorItem(parameters, out _, out var item))
            Item = item;
        if (parameters.Length == 1 && parameters[0] is Plus.HabboHotel.Rooms.Instance.WiredChatTriggerContext chatContext)
        {
            Message = chatContext.Message;
            CommandManager = chatContext.CommandManager;
        }
        else if (parameters.Length > 1)
        {
            Message = Convert.ToString(parameters[1]);
            CommandManager = parameters[1] as CommandManager;
        }
    }

    public object[] Parameters { get; }
    public Habbo? Actor { get; }
    public Item? Item { get; }
    public string? Message { get; }
    public CommandManager? CommandManager { get; }
}
