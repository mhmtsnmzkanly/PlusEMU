using Plus.HabboHotel.Rooms.Chat.Commands;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Instance;

internal sealed class WiredChatTriggerContext
{
    public WiredChatTriggerContext(Habbo actor, string? message = null, CommandManager? commandManager = null)
    {
        Actor = actor;
        Message = message;
        CommandManager = commandManager;
    }

    public Habbo Actor { get; }
    public string? Message { get; }
    public CommandManager? CommandManager { get; }
}
