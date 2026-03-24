using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.PathFinding;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator.Fun;

internal class AllEyesOnMeCommand : IChatCommand
{
    public string Key => "alleyesonme";
    public string PermissionRequired => "command_alleyesonme";

    public string Parameters => "";

    public string Description => "Want some attention? Make everyone face you!";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        var thisUser = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (thisUser == null)
            return;
        var users = room.GetRoomUserManager().GetRoomUsers();
        foreach (var u in users.ToList())
        {
            if (u == null || habbo.Id == u.UserId)
                continue;
            u.SetRot(Rotation.Calculate(u.X, u.Y, thisUser.X, thisUser.Y), false);
        }
    }
}
