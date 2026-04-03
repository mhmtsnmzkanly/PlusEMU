using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class LayCommand : IChatCommand
{
    public string Key => "lay";
    public string PermissionRequired => "command_lay";

    public string Parameters => "";

    public string Description => "Allows you to lay down in the room, without needing a bed.";

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Effects == null)
            return;

        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var user) || user == null)
            return;
        if (!room.GetGameMap().ValidTile(user.X + 2, user.Y + 2) && !room.GetGameMap().ValidTile(user.X + 1, user.Y + 1))
        {
            session.SendWhisper("Oops, cannot lay down here - try elsewhere!");
            return;
        }
        if (user.Statusses.ContainsKey("sit") || user.IsSitting || user.RidingHorse || user.IsWalking)
            return;
        if (habbo.Effects.CurrentEffect > 0)
            habbo.Effects.ApplyEffect(0);
        if (!user.Statusses.ContainsKey("lay"))
        {
            if (user.RotBody % 2 == 0)
            {
                user.Statusses["lay"] = "1.0 null";
                user.Z -= 0.35;
                user.IsLying = true;
                user.UpdateNeeded = true;
            }
            else
            {
                user.RotBody--; //
                user.Statusses["lay"] = "1.0 null";
                user.Z -= 0.35;
                user.IsLying = true;
                user.UpdateNeeded = true;
            }
        }
        else
        {
            user.Z += 0.35;
            user.Statusses.Remove("lay");
            user.Statusses.Remove("1.0");
            user.IsLying = false;
            user.UpdateNeeded = true;
        }
    }
}
