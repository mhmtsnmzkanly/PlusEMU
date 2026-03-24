using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class RoomMuteCommand : IChatCommand
{
    public string Key => "roommute";
    public string PermissionRequired => "command_roommute";

    public string Parameters => "%message%";

    public string Description => "Mute the room with a reason.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var username = session.GetHabbo()?.Username;
        var message = CommandManager.MergeParams(parameters, 1);
        if (string.IsNullOrWhiteSpace(message))
        {
            session.SendWhisper("Please provide a reason for muting the room to show to the users.");
            return;
        }
        if (!room.RoomMuted)
            room.RoomMuted = true;
        var roomUsers = room.GetRoomUserManager().GetRoomUsers();
        if (roomUsers.Count > 0)
        {
            var whisperMessage = $"This room has been muted because: {message}";
            foreach (var user in roomUsers)
            {
                var targetHabbo = user?.GetClient()?.GetHabbo();
                if (targetHabbo == null || targetHabbo.Username == username)
                    continue;
                user.GetClient().SendWhisper(whisperMessage);
            }
        }
    }
}
