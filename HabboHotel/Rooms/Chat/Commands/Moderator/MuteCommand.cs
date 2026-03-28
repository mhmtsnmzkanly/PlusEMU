using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class MuteCommand : ITargetChatCommand
{
    private readonly IDatabase _database;
    public string Key => "mute";
    public string PermissionRequired => "command_mute";

    public string Parameters => "%username% %time%";

    public string Description => "Mute another user for a certain amount of time.";

    public bool MustBeInSameRoom => false;

    public MuteCommand(IDatabase database)
    {
        _database = database;
    }

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        var habbo = session.GetHabbo();
        var permissions = habbo?.Permissions;
        if ((target.Permissions?.HasRight("mod_tool") ?? false) && !(permissions?.HasRight("mod_mute_any") ?? false))
        {
            session.SendWhisper("Oops, you cannot mute that user.");
            return Task.CompletedTask;
        }
        if (double.TryParse(parameters[0], out var time))
        {
            if (time > 600 && !(permissions?.HasRight("mod_mute_limit_override") ?? false))
                time = 600;
            using var connection = _database.Connection();
            connection.Execute("UPDATE `users` SET `time_muted` = @time WHERE `id` = @id LIMIT 1", new { time = time, id = target.Id });
            if (target.TryGetClient(out var targetClient))
            {
                target.TimeMuted = time;
                targetClient.SendNotification($"You have been muted by a moderator for {time} seconds!");
            }
            session.SendWhisper($"You have successfully muted {target.Username} for {time} seconds.");
        }
        else
            session.SendWhisper("Please enter a valid integer.");

        return Task.CompletedTask;
    }
}
