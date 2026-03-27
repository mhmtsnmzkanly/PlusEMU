using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.RCON.Commands.User;

internal class GiveUserCurrencyCommand : IRconCommand
{
    private readonly IDatabase _database;
    private readonly IGameClientManager _gameClientManager;
    public string Description => "This command is used to give a user a specified amount of a specified currency.";
    public string Key => "give_user_currency";
    public string Parameters => "%userId% %currency% %amount%";

    public GiveUserCurrencyCommand(IDatabase database, IGameClientManager gameClientManager)
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
        if (!int.TryParse(parameters[2], out var amount)) return Task.FromResult(false);
        using var db = _database.Connection();
        switch (currency)
        {
            default: return Task.FromResult(false);
            case "coins":
            case "credits":
                client.GetHabbo().Credits += amount;
                db.Execute("UPDATE `users` SET `credits` = @v WHERE `id` = @id LIMIT 1", new { v = client.GetHabbo().Credits, id = userId });
                client.Send(new CreditBalanceComposer(client.GetHabbo().Credits));
                break;
            case "pixels":
            case "duckets":
                client.GetHabbo().Duckets += amount;
                db.Execute("UPDATE `users` SET `activity_points` = @v WHERE `id` = @id LIMIT 1", new { v = client.GetHabbo().Duckets, id = userId });
                client.Send(new HabboActivityPointNotificationComposer(client.GetHabbo().Duckets, amount));
                break;
            case "diamonds":
                client.GetHabbo().Diamonds += amount;
                db.Execute("UPDATE `users` SET `vip_points` = @v WHERE `id` = @id LIMIT 1", new { v = client.GetHabbo().Diamonds, id = userId });
                client.Send(new HabboActivityPointNotificationComposer(client.GetHabbo().Diamonds, 0, 5));
                break;
            case "gotw":
                client.GetHabbo().GotwPoints += amount;
                db.Execute("UPDATE `users` SET `gotw_points` = @v WHERE `id` = @id LIMIT 1", new { v = client.GetHabbo().GotwPoints, id = userId });
                client.Send(new HabboActivityPointNotificationComposer(client.GetHabbo().GotwPoints, 0, 103));
                break;
        }
        return Task.FromResult(true);
    }
}