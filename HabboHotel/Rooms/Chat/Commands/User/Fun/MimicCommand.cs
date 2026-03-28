using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class MimicCommand : ITargetChatCommand
{
    private readonly IDatabase _database;
    public string Key => "mimic";
    public string PermissionRequired => "command_mimic";

    public string Parameters => "%username%";

    public string Description => "Liking someone elses swag? Copy it!";
    public bool MustBeInSameRoom => true;

    public MimicCommand(IDatabase database)
    {
        _database = database;
    }

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.IsInRoom(room))
            return Task.CompletedTask;

        if (!target.AllowMimic)
        {
            session.SendWhisper("Oops, you cannot mimic this user - sorry!");
            return Task.CompletedTask;
        }
        var targetUser = room.GetRoomUserManager().GetRoomUserByHabbo(target.Id);
        if (targetUser == null)
        {
            session.SendWhisper("An error occoured whilst finding that user, maybe they're not online or in this room.");
            return Task.CompletedTask;
        }
        var targetClient = targetUser.GetClient();
        var targetHabbo = targetClient?.GetHabbo();
        if (targetHabbo == null)
            return Task.CompletedTask;
        habbo.Gender = targetHabbo.Gender;
        habbo.Look = targetHabbo.Look;
        using var connection = _database.Connection();
        connection.Execute("UPDATE `users` SET `gender` = @gender, `look` = @look WHERE `id` = @id LIMIT 1", new { gender = habbo.Gender, look = habbo.Look, id = habbo.Id });
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user != null)
        {
            session.Send(new AvatarAspectUpdateComposer(habbo.Look, habbo.Gender));
            session.Send(new UserChangeComposer(user, true));
            room.SendPacket(new UserChangeComposer(user, false));
        }
        return Task.CompletedTask;
    }
}
