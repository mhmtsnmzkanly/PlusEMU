using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class RoomUnmuteCommand : IChatCommand
{
    public string Key => "roomunmute";
    public string PermissionRequired => "command_unroommute";

    public string Parameters => "";

    public string Description => "Unmute the room.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var username = session.GetHabbo()?.Username;
        if (!room.RoomMuted)
        {
            session.SendWhisper("This room isn't muted.");
            return;
        }
        room.RoomMuted = false;
        var roomUsers = room.GetRoomUserManager().GetRoomUsers();
        if (roomUsers.Count > 0)
        {
            foreach (var user in roomUsers)
            {
                var targetHabbo = user?.GetClient()?.GetHabbo();
                if (targetHabbo == null || targetHabbo.Username == username)
                    continue;
                user.GetClient().SendWhisper("This room has been un-muted .");
            }
        }
    }
}
