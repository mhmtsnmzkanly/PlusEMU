using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Administrator;

internal class CarryCommand : IChatCommand
{
    public string Key => "carry";
    public string PermissionRequired => "command_carry";

    public string Parameters => "%ItemId%";

    public string Description => "Allows you to carry a hand item";

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var itemId = 0;
        if (!int.TryParse(Convert.ToString(parameters[0]), out itemId))
        {
            session.SendWhisper("Please enter a valid integer.");
            return;
        }
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var user) || user == null)
            return;
        user.CarryItem(itemId);
    }
}
