using Dapper;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Permissions;
using Plus.HabboHotel.Users.Permissions;

namespace Plus.Communication.RCON.Commands.User;

internal class ReloadUserRankCommand : IRconCommand
{
    private readonly IDatabase _database;
    private readonly IGameClientManager _gameClientManager;
    private readonly IModerationManager _moderationManager;
    private readonly IPermissionManager _permissionManager;
    public string Description => "This command is used to reload a users rank and permissions.";
    public string Key => "reload_user_rank";
    public string Parameters => "%userId%";

    public ReloadUserRankCommand(IDatabase database, IGameClientManager gameClientManager, IModerationManager moderationManager, IPermissionManager permissionManager)
    {
        _database = database;
        _gameClientManager = gameClientManager;
        _moderationManager = moderationManager;
        _permissionManager = permissionManager;
    }

    public Task<bool> TryExecute(string[] parameters)
    {
        if (!int.TryParse(parameters[0], out var userId)) return Task.FromResult(false);
        var client = _gameClientManager.GetClientByUserId(userId);
        var habbo = client?.GetHabbo();
        if (habbo == null) return Task.FromResult(false);
        using var db = _database.Connection();
        habbo.Rank = db.QueryFirstOrDefault<int>("SELECT `rank` FROM `users` WHERE `id` = @userId LIMIT 1", new { userId });
        habbo.Permissions = new(_permissionManager.GetPermissionsForPlayer(habbo), _permissionManager.GetCommandsForPlayer(habbo));
        if (habbo.Permissions?.HasRight("mod_tickets") == true)
        {
            client?.Send(new ModeratorInitComposer(
                _moderationManager.UserMessagePresets,
                _moderationManager.RoomMessagePresets,
                _moderationManager.GetTickets));
        }
        return Task.FromResult(true);
    }
}
