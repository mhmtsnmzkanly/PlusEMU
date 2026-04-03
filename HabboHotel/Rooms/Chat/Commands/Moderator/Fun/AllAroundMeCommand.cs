using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator.Fun;

internal class AllAroundMeCommand : IChatCommand
{
    public string Key => "allaroundme";
    public string PermissionRequired => "command_allaroundme";

    public string Parameters => "";

    public string Description => "Need some attention? Pull all of the users to you.";

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var user) || user == null)
            return;
        var users = room.GetRoomUserManager().GetRoomUsers();
        foreach (var u in users.ToList())
        {
            if (u == null || habbo.Id == u.UserId)
                continue;
            u.MoveTo(user.X, user.Y, true);
        }
    }
}
