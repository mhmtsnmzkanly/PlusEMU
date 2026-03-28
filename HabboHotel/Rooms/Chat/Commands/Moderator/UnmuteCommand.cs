using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class UnmuteCommand : ITargetChatCommand
{
    private readonly IDatabase _database;
    public string Key => "unmute";
    public string PermissionRequired => "command_unmute";

    public string Parameters => "%username%";

    public string Description => "Unmute a currently muted user.";

    public bool MustBeInSameRoom => false;

    public UnmuteCommand(IDatabase database)
    {
        _database = database;
    }

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        var habbo = session.GetHabbo();
        var username = habbo?.Username ?? string.Empty;
        using var connection = _database.Connection();
        connection.Execute("UPDATE `users` SET `time_muted` = '0' WHERE `id` = @id LIMIT 1", new { id = target.Id });
        target.TimeMuted = 0;
        if (target.TryGetClient(out var targetClient))
            targetClient.SendNotification($"You have been un-muted by {username}!");
        session.SendWhisper($"You have successfully un-muted {target.Username}!");
        return Task.CompletedTask;
    }
}
