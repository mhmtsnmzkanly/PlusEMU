using Dapper;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Permissions;
using Plus.HabboHotel.Users.Permissions;

namespace Plus.Communication.RCON.Commands.User;

internal class ReloadUserVipRankCommand : IRconCommand
{
    private readonly IDatabase _database;
    private readonly IGameClientManager _gameClientManager;
    private readonly IPermissionManager _permissionManager;
    public string Description => "This command is used to reload a users VIP rank and permissions.";
    public string Key => "reload_user_vip_rank";
    public string Parameters => "%userId%";

    public ReloadUserVipRankCommand(IDatabase database, IGameClientManager gameClientManager, IPermissionManager permissionManager)
    {
        _database = database;
        _gameClientManager = gameClientManager;
        _permissionManager = permissionManager;
    }

    public Task<bool> TryExecute(string[] parameters)
    {
        if (!int.TryParse(parameters[0], out var userId)) return Task.FromResult(false);
        var client = _gameClientManager.GetClientByUserId(userId);
        var habbo = client?.GetHabbo();
        if (habbo == null) return Task.FromResult(false);
        using var db = _database.Connection();
        habbo.VipRank = db.QueryFirstOrDefault<int>("SELECT `rank_vip` FROM `users` WHERE `id` = @userId LIMIT 1", new { userId });
        habbo.Permissions = new(_permissionManager.GetPermissionsForPlayer(habbo), _permissionManager.GetCommandsForPlayer(habbo));
        return Task.FromResult(true);
    }
}
