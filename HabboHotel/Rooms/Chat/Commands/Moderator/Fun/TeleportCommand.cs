using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator.Fun;

internal class TeleportCommand : IChatCommand
{
    public string Key => "teleport";
    public string PermissionRequired => "command_teleport";

    public string Parameters => "";

    public string Description => "The ability to teleport anywhere within the room.";

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return;
        user.TeleportEnabled = !user.TeleportEnabled;
        room.GetGameMap().GenerateMaps();
    }
}
