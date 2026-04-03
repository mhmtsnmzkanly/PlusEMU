using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator.Fun;

internal class OverrideCommand : IChatCommand
{
    public string Key => "override";
    public string PermissionRequired => "command_override";

    public string Parameters => "";

    public string Description => "Gives you the ability to walk over anything.";

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var user) || user == null)
            return;
        user.AllowOverride = !user.AllowOverride;
        session.SendWhisper("Override mode updated.");
    }
}
