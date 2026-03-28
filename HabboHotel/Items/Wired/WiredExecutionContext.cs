using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired;

public class WiredExecutionContext
{
    protected WiredExecutionContext()
    {
    }

    public WiredExecutionContext(params object[] parameters)
    {
        if (WiredContextResolver.TryGetActor(parameters, out var actor))
            Actor = actor;
        if (WiredContextResolver.TryGetActorItem(parameters, out _, out var item))
            Item = item;
        if (parameters.Length == 1 && parameters[0] is Plus.HabboHotel.Rooms.Instance.WiredChatTriggerContext chatContext)
        {
            Message = chatContext.Message;
        }
        else if (parameters.Length > 1)
        {
            Message = Convert.ToString(parameters[1]);
        }
    }

    public Habbo? Actor { get; protected init; }
    public Item? Item { get; protected init; }
    public string? Message { get; protected init; }
}
