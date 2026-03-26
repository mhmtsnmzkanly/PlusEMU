using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;
using Plus.HabboHotel.Groups;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Administrator;

internal class DeleteGroupCommand : IChatCommand
{
    private readonly IGroupManager _groupManager;
    private readonly IRoomManager _roomManager;
    private readonly IDatabase _database;
    public string Key => "deletegroup";
    public string PermissionRequired => "command_delete_group";

    public string Parameters => "";

    public string Description => "Delete a group from the database and cache.";

    public DeleteGroupCommand(IGroupManager groupManager, IRoomManager roomManager, IDatabase database)
    {
        _groupManager = groupManager;
        _roomManager = roomManager;
        _database = database;
    }

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        room = habbo?.CurrentRoom!;
        if (room == null)
            return;
        if (room.Group == null)
        {
            session.SendWhisper("Oops, there is no group here?");
            return;
        }
        using var connection = _database.Connection();
        connection.Execute("DELETE FROM `groups` WHERE `id` = @id", new { id = room.Group.Id });
        connection.Execute("DELETE FROM `group_memberships` WHERE `group_id` = @id", new { id = room.Group.Id });
        connection.Execute("DELETE FROM `group_requests` WHERE `group_id` = @id", new { id = room.Group.Id });
        connection.Execute("UPDATE `rooms` SET `group_id` = '0' WHERE `group_id` = @id LIMIT 1", new { id = room.Group.Id });
        connection.Execute("UPDATE `user_statistics` SET `groupid` = '0' WHERE `groupid` = @id LIMIT 1", new { id = room.Group.Id });
        connection.Execute("DELETE FROM `items_groups` WHERE `group_id` = @id", new { id = room.Group.Id });
        _groupManager.DeleteGroup(room.Group.Id);
        room.Group = null;
        _roomManager.UnloadRoom(room.Id);
        session.SendNotification("Success, group deleted.");
    }
}
