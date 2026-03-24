using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class StandCommand : IChatCommand
{
    public string Key => "stand";
    public string PermissionRequired => "command_stand";

    public string Parameters => "";

    public string Description => "Allows you to stand up if not stood already.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        var username = habbo?.Username;
        if (string.IsNullOrEmpty(username))
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(username);
        if (user == null)
            return;
        if (user.IsSitting)
        {
            user.Statusses.Remove("sit");
            user.Z += 0.35;
            user.IsSitting = false;
            user.UpdateNeeded = true;
        }
        else if (user.IsLying)
        {
            user.Statusses.Remove("lay");
            user.Z += 0.35;
            user.IsLying = false;
            user.UpdateNeeded = true;
        }
    }
}
