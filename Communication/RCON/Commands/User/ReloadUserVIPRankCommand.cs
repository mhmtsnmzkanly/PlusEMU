using Dapper;
using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.RCON.Commands.User;

internal class ReloadUserVipRankCommand : IRconCommand
{
    private readonly IDatabase _database;
    private readonly IGameClientManager _gameClientManager;
    public string Description => "This command is used to reload a users VIP rank and permissions.";
    public string Key => "reload_user_vip_rank";
    public string Parameters => "%userId%";

    public ReloadUserVipRankCommand(IDatabase database, IGameClientManager gameClientManager)
    {
        _database = database;
        _gameClientManager = gameClientManager;
    }

    public Task<bool> TryExecute(string[] parameters)
    {
        if (!int.TryParse(parameters[0], out var userId)) return Task.FromResult(false);
        var client = _gameClientManager.GetClientByUserId(userId);
        var habbo = client?.GetHabbo();
        if (habbo == null) return Task.FromResult(false);
        using var db = _database.Connection();
        habbo.VipRank = db.QueryFirstOrDefault<int>("SELECT `rank_vip` FROM `users` WHERE `id` = @userId LIMIT 1", new { userId });
        habbo.Permissions?.Init(habbo);
        return Task.FromResult(true);
    }
}
