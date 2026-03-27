using System;
using System.Text;
using Dapper;
using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class UserInfoCommand : IChatCommand
{
    private readonly IDatabase _database;
    private readonly IGameClientManager _gameClientManager;
    public string Key => "userinfo";
    public string PermissionRequired => "command_user_info";

    public string Parameters => "%username%";

    public string Description => "View another users profile information.";

    public UserInfoCommand(IDatabase database, IGameClientManager gameClientManager)
    {
        _database = database;
        _gameClientManager = gameClientManager;
    }

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        if (parameters.Length == 1)
        {
            session.SendWhisper("Please enter the username of the user you wish to view.");
            return;
        }
        var username = parameters[1];
        
        dynamic? userData = null;
        using (var connection = _database.Connection())
        {
            userData = connection.QuerySingleOrDefault("SELECT `id`,`username`,`mail`,`rank`,`motto`,`credits`,`activity_points`,`vip_points`,`gotw_points`,`online`,`rank_vip` FROM users WHERE `username` = @Username LIMIT 1", new { Username = username });
        }
        
        if (userData == null)
        {
            session.SendNotification($"Oops, there is no user in the database with that username ({username})!");
            return;
        }

        dynamic? userInfo = null;
        using (var connection = _database.Connection())
        {
            var userId = Convert.ToInt32(userData.id);
            userInfo = connection.QuerySingleOrDefault("SELECT * FROM `user_info` WHERE `user_id` = @UserId LIMIT 1", new { UserId = userId });
            
            if (userInfo == null)
            {
                connection.Execute("INSERT INTO `user_info` (`user_id`) VALUES (@UserId)", new { UserId = userId });
                userInfo = connection.QuerySingleOrDefault("SELECT * FROM `user_info` WHERE `user_id` = @UserId LIMIT 1", new { UserId = userId });
            }
        }
        
        var targetClient = _gameClientManager.GetClientByUsername(username);
        var targetHabbo = targetClient?.GetHabbo();
        var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Convert.ToDouble(userInfo.trading_locked));
        var habboInfo = new StringBuilder();
        habboInfo.Append($"{Convert.ToString(userData.username)}'s account:\r\r");
        habboInfo.Append("Generic Info:\r");
        habboInfo.Append($"ID: {Convert.ToInt32(userData.id)}\r");
        habboInfo.Append($"Rank: {Convert.ToInt32(userData.rank)}\r");
        habboInfo.Append($"VIP Rank: {Convert.ToInt32(userData.rank_vip)}\r");
        habboInfo.Append($"Email: {Convert.ToString(userData.mail)}\r");
        habboInfo.Append($"Online Status: {(targetClient != null ? "True" : "False")}\r\r");
        habboInfo.Append("Currency Info:\r");
        habboInfo.Append($"Credits: {Convert.ToInt32(userData.credits)}\r");
        habboInfo.Append($"Duckets: {Convert.ToInt32(userData.activity_points)}\r");
        habboInfo.Append($"Diamonds: {Convert.ToInt32(userData.vip_points)}\r");
        habboInfo.Append($"GOTW Points: {Convert.ToInt32(userData.gotw_points)}\r\r");
        habboInfo.Append("Moderation Info:\r");
        habboInfo.Append($"Bans: {Convert.ToInt32(userInfo.bans)}\r");
        habboInfo.Append($"CFHs Sent: {Convert.ToInt32(userInfo.cfhs)}\r");
        habboInfo.Append($"Abusive CFHs: {Convert.ToInt32(userInfo.cfhs_abusive)}\r");
        habboInfo.Append($"Trading Locked: {(Convert.ToInt32(userInfo.trading_locked) == 0 ? "No outstanding lock" : $"Expiry: {origin:dd/MM/yyyy}")}\r");
        habboInfo.Append($"Amount of trading locks: {Convert.ToInt32(userInfo.trading_locks_count)}\r\r");
        
        if (targetHabbo != null)
        {
            habboInfo.Append("Current Session:\r");
            if (!targetHabbo.InRoom || targetHabbo.CurrentRoom == null)
                habboInfo.Append("Currently not in a room.\r");
            else
            {
                var currentRoom = targetHabbo.CurrentRoom;
                habboInfo.Append($"Room: {currentRoom.Name} ({currentRoom.RoomId})\r");
                habboInfo.Append($"Room Owner: {currentRoom.OwnerName}\r");
                habboInfo.Append($"Current Visitors: {currentRoom.UserCount}/{currentRoom.UsersMax}");
            }
        }
        session.SendNotification(habboInfo.ToString());
    }
}
