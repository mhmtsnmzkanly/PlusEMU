using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class RoomAlertCommand : IChatCommand
{
    public string Key => "roomalert";
    public string PermissionRequired => "command_room_alert";

    public string Parameters => "%message%";

    public string Description => "Send a message to the users in this room.";

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null)
            return;

        if (!parameters.Any())
        {
            session.SendWhisper("Please enter a message you'd like to send to the room.");
            return;
        }
        if (!habbo.Permissions.HasRight("mod_alert") && room.OwnerId != habbo.Id)
        {
            session.SendWhisper("You can only Room Alert in your own room!");
            return;
        }
        var message = $"{habbo.Username} alerted the room with the following message:\n\n{CommandManager.MergeParams(parameters)}";
        foreach (var roomUser in room.GetRoomUserManager().GetRoomUsers())
        {
            var targetClient = roomUser?.GetClient();
            if (roomUser == null || targetClient == null || habbo.Id == roomUser.UserId)
                continue;
            targetClient.SendNotification(message);
        }
        session.SendWhisper("Message successfully sent to the room.");
    }
}
