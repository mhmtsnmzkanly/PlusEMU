using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class DisableWhispersCommand : IChatCommand
{
    public string Key => "disablewhispers";
    public string PermissionRequired => "command_disable_whispers";

    public string Parameters => "";

    public string Description => "Allows you to enable or disable the ability to receive whispers.";

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        habbo.ReceiveWhispers = !habbo.ReceiveWhispers;
        session.SendWhisper($"You're {(habbo.ReceiveWhispers ? "now" : "no longer")} receiving whispers!");
    }
}
