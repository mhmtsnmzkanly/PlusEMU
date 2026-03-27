using Dapper;
using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.RCON.Commands.User;

internal class SyncUserCurrencyCommand : IRconCommand
{
    private readonly IDatabase _database;
    private readonly IGameClientManager _gameClientManager;
    public string Description => "This command is used to sync a users specified currency to the database.";
    public string Key => "sync_user_currency";
    public string Parameters => "%userId% %currency%";

    public SyncUserCurrencyCommand(IDatabase database, IGameClientManager gameClientManager)
    {
        _database = database;
        _gameClientManager = gameClientManager;
    }

    public Task<bool> TryExecute(string[] parameters)
    {
        if (!int.TryParse(parameters[0], out var userId)) return Task.FromResult(false);
        var client = _gameClientManager.GetClientByUserId(userId);
        if (client == null || client.GetHabbo() == null) return Task.FromResult(false);
        if (string.IsNullOrEmpty(Convert.ToString(parameters[1]))) return Task.FromResult(false);
        var currency = Convert.ToString(parameters[1]);
        using var db = _database.Connection();
        switch (currency)
        {
            default: return Task.FromResult(false);
            case "coins":
            case "credits":
                db.Execute("UPDATE `users` SET `credits` = @v WHERE `id` = @id LIMIT 1", new { v = client.GetHabbo().Credits, id = userId });
                break;
            case "pixels":
            case "duckets":
                db.Execute("UPDATE `users` SET `activity_points` = @v WHERE `id` = @id LIMIT 1", new { v = client.GetHabbo().Duckets, id = userId });
                break;
            case "diamonds":
                db.Execute("UPDATE `users` SET `vip_points` = @v WHERE `id` = @id LIMIT 1", new { v = client.GetHabbo().Diamonds, id = userId });
                break;
            case "gotw":
                db.Execute("UPDATE `users` SET `gotw_points` = @v WHERE `id` = @id LIMIT 1", new { v = client.GetHabbo().GotwPoints, id = userId });
                break;
        }
        return Task.FromResult(true);
    }
}