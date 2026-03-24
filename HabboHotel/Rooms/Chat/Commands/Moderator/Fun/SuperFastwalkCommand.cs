using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator.Fun;

internal class SuperFastwalkCommand : IChatCommand
{
    public string Key => "superfastwalk";
    public string PermissionRequired => "command_super_fastwalk";

    public string Parameters => "";

    public string Description => "Gives you the ability to walk very very fast.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return;
        user.SuperFastWalking = !user.SuperFastWalking;
        if (user.FastWalking)
            user.FastWalking = false;
        session.SendWhisper("Walking mode updated.");
    }
}
