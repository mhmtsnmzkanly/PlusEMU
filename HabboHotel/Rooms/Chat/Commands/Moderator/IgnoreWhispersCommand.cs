using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class IgnoreWhispersCommand : IChatCommand
{
    public string Key => "ignorewhispers";
    public string PermissionRequired => "command_ignore_whispers";

    public string Parameters => "";

    public string Description => "Allows you to ignore all of the whispers in the room, except from your own.";

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        habbo.IgnorePublicWhispers = !habbo.IgnorePublicWhispers;
        session.SendWhisper($"You're {(habbo.IgnorePublicWhispers ? "now" : "no longer")} ignoring public whispers!");
    }
}
