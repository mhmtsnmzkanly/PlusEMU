using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.RCON.Commands.User;

internal class ReloadUserRankCommand : IRconCommand
{
    private readonly IDatabase _database;
    private readonly IGameClientManager _gameClientManager;
    private readonly IModerationManager _moderationManager;
    public string Description => "This command is used to reload a users rank and permissions.";

    public string Key => "reload_user_rank";
    public string Parameters => "%userId%";

    public ReloadUserRankCommand(IDatabase database, IGameClientManager gameClientManager, IModerationManager moderationManager)
    {
        _database = database;
        _gameClientManager = gameClientManager;
        _moderationManager = moderationManager;
    }

    public Task<bool> TryExecute(string[] parameters)
    {
        if (!int.TryParse(parameters[0], out var userId))
            return Task.FromResult(false);
        var client = _gameClientManager.GetClientByUserId(userId);
        var habbo = client?.GetHabbo();
        if (habbo == null)
            return Task.FromResult(false);
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery("SELECT `rank` FROM `users` WHERE `id` = @userId LIMIT 1");
            dbClient.AddParameter("userId", userId);
            habbo.Rank = dbClient.GetInteger();
        }
        habbo.Permissions?.Init(habbo);
        if (habbo.Permissions?.HasRight("mod_tickets") == true)
        {
            client.Send(new ModeratorInitComposer(
                _moderationManager.UserMessagePresets,
                _moderationManager.RoomMessagePresets,
                _moderationManager.GetTickets));
        }
        return Task.FromResult(true);
    }
}
