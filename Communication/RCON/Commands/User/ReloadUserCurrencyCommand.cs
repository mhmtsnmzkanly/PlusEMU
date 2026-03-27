using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.RCON.Commands.User;

internal class ReloadUserCurrencyCommand : IRconCommand
{
    private readonly IDatabase _database;
    private readonly IGameClientManager _gameClientManager;
    public string Description => "This command is used to update the users currency from the database.";
    public string Key => "reload_user_currency";
    public string Parameters => "%userId% %currency%";

    public ReloadUserCurrencyCommand(IDatabase database, IGameClientManager gameClientManager)
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
                client.GetHabbo().Credits = db.QueryFirstOrDefault<int>("SELECT `credits` FROM `users` WHERE `id` = @id LIMIT 1", new { id = userId });
                client.Send(new CreditBalanceComposer(client.GetHabbo().Credits));
                break;
            case "pixels":
            case "duckets":
                var duckets = db.QueryFirstOrDefault<int>("SELECT `activity_points` FROM `users` WHERE `id` = @id LIMIT 1", new { id = userId });
                client.GetHabbo().Duckets = duckets;
                client.Send(new HabboActivityPointNotificationComposer(client.GetHabbo().Duckets, duckets));
                break;
            case "diamonds":
                var diamonds = db.QueryFirstOrDefault<int>("SELECT `vip_points` FROM `users` WHERE `id` = @id LIMIT 1", new { id = userId });
                client.GetHabbo().Diamonds = diamonds;
                client.Send(new HabboActivityPointNotificationComposer(diamonds, 0, 5));
                break;
            case "gotw":
                var gotw = db.QueryFirstOrDefault<int>("SELECT `gotw_points` FROM `users` WHERE `id` = @id LIMIT 1", new { id = userId });
                client.GetHabbo().GotwPoints = gotw;
                client.Send(new HabboActivityPointNotificationComposer(gotw, 0, 103));
                break;
        }
        return Task.FromResult(true);
    }
}