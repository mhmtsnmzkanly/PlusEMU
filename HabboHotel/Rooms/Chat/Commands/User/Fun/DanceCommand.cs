using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class DanceCommand : IChatCommand
{
    public string Key => "dance";
    public string PermissionRequired => "command_dance";

    public string Parameters => "%DanceId%";

    public string Description => "Too lazy to dance the proper way? Do it like this!";

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.IsInRoom(room))
            return;

        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var thisUser) || thisUser == null)
            return;
        if (parameters.Length == 0)
        {
            session.SendWhisper("Please enter an ID of a dance.");
            return;
        }
        if (int.TryParse(parameters[0], out var danceId))
        {
            if (danceId > 4 || danceId < 0)
            {
                session.SendWhisper("The dance ID must be between 0 and 4!");
                return;
            }
            room.SendPacket(new DanceComposer(thisUser, danceId));
        }
        else
            session.SendWhisper("Please enter a valid dance ID.");
    }
}
