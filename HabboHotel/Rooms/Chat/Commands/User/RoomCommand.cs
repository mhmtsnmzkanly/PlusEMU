using System.Text;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;
using Plus.Utilities;
using Plus.HabboHotel.Groups;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class RoomCommand : IChatCommand
{
    private readonly IDatabase _database;
    private readonly IGroupManager _groupManager;

    public string Key => "room";
    public string PermissionRequired => "command_room";

    public string Parameters => "push/pull/enables/respect";

    public string Description => "Gives you the ability to enable or disable basic room commands.";

    public RoomCommand(IDatabase database, IGroupManager groupManager)
    {
        _database = database;
        _groupManager = groupManager;
    }

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        if (!parameters.Any())
        {
            session.SendWhisper("Oops, you must choose a room option to disable.");
            return;
        }
        if (!room.CheckRights(session, true))
        {
            session.SendWhisper("Oops, only the room owner or staff can use this command.");
            return;
        }
        var option = parameters[0];
        switch (option)
        {
            case "list":
            {
                var list = new StringBuilder("");
                list.AppendLine("Room Command List");
                list.AppendLine("-------------------------");
                list.AppendLine($"Pet Morphs: {(room.PetMorphsAllowed ? "enabled" : "disabled")}");
                list.AppendLine($"Pull: {(room.PullEnabled ? "enabled" : "disabled")}");
                list.AppendLine($"Push: {(room.PushEnabled ? "enabled" : "disabled")}");
                list.AppendLine($"Super Pull: {(room.SuperPullEnabled ? "enabled" : "disabled")}");
                list.AppendLine($"Super Push: {(room.SuperPushEnabled ? "enabled" : "disabled")}");
                list.AppendLine($"Respect: {(room.RespectNotificationsEnabled ? "enabled" : "disabled")}");
                list.AppendLine($"Enables: {(room.EnablesEnabled ? "enabled" : "disabled")}");
                session.SendNotification(list.ToString());
                break;
            }
            case "push":
            {
                room.PushEnabled = !room.PushEnabled;
                using var connection = _database.Connection();
                connection.Execute("UPDATE `rooms` SET `push_enabled` = @pushEnabled WHERE `id` = @roomId LIMIT 1", new { roomId = room.Id, pushEnabled = ConvertExtensions.ToStringEnumValue(room.PushEnabled) });
                session.SendWhisper($"Push mode is now {(room.PushEnabled ? "enabled!" : "disabled!")}");
                break;
            }
            case "spush":
            {
                room.SuperPushEnabled = !room.SuperPushEnabled;
                using var connection = _database.Connection();
                connection.Execute("UPDATE `rooms` SET `spush_enabled` = @sPushEnabled WHERE `id` = @roomId LIMIT 1", new { roomId = room.Id, sPushEnabled = ConvertExtensions.ToStringEnumValue(room.SuperPushEnabled) });
                session.SendWhisper($"Super Push mode is now {(room.SuperPushEnabled ? "enabled!" : "disabled!")}");
                break;
            }
            case "spull":
            {
                room.SuperPullEnabled = !room.SuperPullEnabled;
                using var connection = _database.Connection();
                connection.Execute("UPDATE `rooms` SET `spull_enabled` = @sPullEnabled WHERE `id` = @roomId LIMIT 1", new { roomId = room.Id, sPullEnabled = ConvertExtensions.ToStringEnumValue(room.SuperPullEnabled) });
                session.SendWhisper($"Super Pull mode is now {(room.SuperPullEnabled ? "enabled!" : "disabled!")}");
                break;
            }
            case "pull":
            {
                room.PullEnabled = !room.PullEnabled;
                using var connection = _database.Connection();
                connection.Execute("UPDATE `rooms` SET `pull_enabled` = @pullEnabled WHERE `id` = @roomId LIMIT 1", new { roomId = room.Id, pullEnabled = ConvertExtensions.ToStringEnumValue(room.PullEnabled) });
                session.SendWhisper($"Pull mode is now {(room.PullEnabled ? "enabled!" : "disabled!")}");
                break;
            }
            case "enable":
            case "enables":
            {
                room.EnablesEnabled = !room.EnablesEnabled;
                using var connection = _database.Connection();
                connection.Execute("UPDATE `rooms` SET `enables_enabled` = @enablesEnabled WHERE `id` = @roomId LIMIT 1", new { roomId = room.Id, enablesEnabled = ConvertExtensions.ToStringEnumValue(room.EnablesEnabled) });
                session.SendWhisper($"Enables mode set to {(room.EnablesEnabled ? "enabled!" : "disabled!")}");
                break;
            }
            case "respect":
            {
                room.RespectNotificationsEnabled = !room.RespectNotificationsEnabled;
                using var connection = _database.Connection();
                connection.Execute("UPDATE `rooms` SET `respect_notifications_enabled` = @respectNotificationsEnabled WHERE `id` = @roomId LIMIT 1", new { roomId = room.Id, respectNotificationsEnabled = ConvertExtensions.ToStringEnumValue(room.RespectNotificationsEnabled) });
                session.SendWhisper($"Respect notifications mode set to {(room.RespectNotificationsEnabled ? "enabled!" : "disabled!")}");
                break;
            }
            case "pets":
            case "morphs":
            {
                room.PetMorphsAllowed = !room.PetMorphsAllowed;
                using var connection = _database.Connection();
                connection.Execute("UPDATE `rooms` SET `pet_morphs_allowed` = @petMorphsAllowed WHERE `id` = @roomId LIMIT 1", new { roomId = room.Id, petMorphsAllowed = ConvertExtensions.ToStringEnumValue(room.PetMorphsAllowed) });
                session.SendWhisper($"Human pet morphs notifications mode set to {(room.PetMorphsAllowed ? "enabled!" : "disabled!")}");
                if (!room.PetMorphsAllowed)
                {
                    foreach (var user in room.GetRoomUserManager().GetRoomUsers())
                    {
                        if (user == null)
                            continue;
                        var roomUser = user;
                        var targetClient = user?.GetClient();
                        var targetHabbo = targetClient?.GetHabbo();
                        if (targetHabbo == null || targetClient == null)
                            continue;
                        targetClient.SendWhisper("The room owner has disabled the ability to use a pet morph in this room.");
                        if (targetHabbo.PetId > 0)
                        {
                            //Tell the user what is going on.
                            targetClient.SendWhisper("Oops, the room owner has just disabled pet-morphs, un-morphing you.");

                            //Change the users Pet Id.
                            targetHabbo.PetId = 0;

                            //Quickly remove the old user instance.
                            room.SendPacket(new UserRemoveComposer(roomUser.VirtualId));

                            //Add the new one, they won't even notice a thing!!11 8-)
                            room.SendPacket(new UsersComposer(roomUser, _groupManager, room.GetCacheManager()));
                        }
                    }
                }
                break;
            }
        }
    }
}
