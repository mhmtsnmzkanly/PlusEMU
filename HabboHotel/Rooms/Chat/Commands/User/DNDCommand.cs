using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class DndCommand : IChatCommand
{
    public string Key => "dnd";
    public string PermissionRequired => "command_dnd";

    public string Parameters => "";

    public string Description => "Allows you to chose the option to enable or disable console messages.";

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        habbo.AllowConsoleMessages = !habbo.AllowConsoleMessages;
        session.SendWhisper($"You're {(habbo.AllowConsoleMessages ? "now" : "no longer")} accepting console messages.");
    }
}
