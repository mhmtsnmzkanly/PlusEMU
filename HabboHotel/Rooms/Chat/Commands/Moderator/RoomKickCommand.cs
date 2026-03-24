using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class RoomKickCommand : IChatCommand
{
    public string Key => "roomkick";
    public string PermissionRequired => "command_room_kick";

    public string Parameters => "%message%";

    public string Description => "Kick the room and provide a message to the users.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        var message = CommandManager.MergeParams(parameters);
        if (string.IsNullOrWhiteSpace(message))
        {
            session.SendWhisper("Please provide a reason to the users for this room kick.");
            return;
        }
        foreach (var roomUser in room.GetRoomUserManager().GetUserList().ToList())
        {
            var targetClient = roomUser?.GetClient();
            var targetHabbo = targetClient?.GetHabbo();
            if (roomUser == null || roomUser.IsBot || targetHabbo == null || targetClient == null ||
                (targetHabbo.Permissions?.HasRight("mod_tool") ?? false) || targetHabbo.Id == habbo.Id)
                continue;
            targetClient.SendNotification($"You have been kicked by a moderator: {message}");
            room.GetRoomUserManager().RemoveUserFromRoom(targetClient, true);
        }
        session.SendWhisper("Successfully kicked all users from the room.");
    }
}
