using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator.Fun;

internal class FastwalkCommand : IChatCommand
{
    public string Key => "fastwalk";
    public string PermissionRequired => "command_fastwalk";

    public string Parameters => "";

    public string Description => "Gives you the ability to walk very fast.";

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var user) || user == null)
            return;
        user.FastWalking = !user.FastWalking;
        if (user.SuperFastWalking)
            user.SuperFastWalking = false;
        session.SendWhisper("Walking mode updated.");
    }
}
