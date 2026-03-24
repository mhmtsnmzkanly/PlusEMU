using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class MoonwalkCommand : IChatCommand
{
    public string Key => "moonwalk";
    public string PermissionRequired => "command_moonwalk";

    public string Parameters => "";

    public string Description => "Wear the shoes of Michael Jackson.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return;
        user.MoonwalkEnabled = !user.MoonwalkEnabled;
        if (user.MoonwalkEnabled)
            session.SendWhisper("Moonwalk enabled!");
        else
            session.SendWhisper("Moonwalk disabled!");
    }
}
