using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class TradeBanCommand : ITargetChatCommand
{
    private readonly IDatabase _database;
    public string Key => "tradeban";
    public string PermissionRequired => "command_trade_ban";

    public string Parameters => "%target% %length%";

    public string Description => "Trade ban another user.";

    public bool MustBeInSameRoom => false;

    public TradeBanCommand(IDatabase database)
    {
        _database = database;
    }

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        if (!parameters.Any())
        {
            session.SendWhisper("Please define tohe amount of days. Use 0 to reset.");
            return Task.CompletedTask;
        }

        if (Convert.ToDouble(parameters[0]) == 0)
        {
            using var connection = _database.Connection();
            connection.Execute("UPDATE `user_info` SET `trading_locked` = '0' WHERE `user_id` = @id LIMIT 1", new { id = target.Id });
            if (target.TryGetClient(out var targetClient))
            {
                target.TradingLockExpiry = 0;
                targetClient.SendNotification("Your outstanding trade ban has been removed.");
            }
            session.SendWhisper($"You have successfully removed {target.Username}'s trade ban.");
            return Task.CompletedTask;
        }
        if (double.TryParse(parameters[0], out var days))
        {
            if (days < 1)
                days = 1;
            if (days > 365)
                days = 365;
            var length = PlusEnvironment.GetUnixTimestamp() + days * 86400;
            using var connection = _database.Connection();
            connection.Execute("UPDATE `user_info` SET `trading_locked` = @length, `trading_locks_count` = `trading_locks_count` + '1' WHERE `user_id` = @id LIMIT 1", new { length = length, id = target.Id });
            if (target.TryGetClient(out var targetClient))
            {
                target.TradingLockExpiry = length;
                targetClient.SendNotification($"You have been trade banned for {days} day(s)!");
            }
            session.SendWhisper($"You have successfully trade banned {target.Username} for {days} day(s).");
        }
        else
            session.SendWhisper("Please enter a valid integer.");
        return Task.CompletedTask;
    }
}
