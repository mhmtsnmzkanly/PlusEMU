using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.RCON.Commands.User;

internal class GiveUserBadgeCommand : IRconCommand
{
    private readonly IBadgeManager _badgeManager;
    private readonly IGameClientManager _gameClientManager;
    public string Description => "This command is used to give a user a badge.";

    public string Key => "give_user_badge";
    public string Parameters => "%userId% %badgeId%";

    public GiveUserBadgeCommand(IBadgeManager badgeManager, IGameClientManager gameClientManager)
    {
        _badgeManager = badgeManager;
        _gameClientManager = gameClientManager;
    }

    public async Task<bool> TryExecute(string[] parameters)
    {
        if (!int.TryParse(parameters[0], out var userId))
            return false;
        var client = _gameClientManager.GetClientByUserId(userId);
        var habbo = client?.GetHabbo();
        if (habbo == null)
            return false;

        // Validate the badge
        var badge = Convert.ToString(parameters[1]);
        if (string.IsNullOrEmpty(badge))
            return false;
        var badges = habbo.Inventory?.Badges;
        if (badges != null && !badges.HasBadge(badge))
        {
            await _badgeManager.GiveBadge(habbo, badge);
            client?.Send(new BroadcastMessageAlertComposer("You have been given a new badge!"));
        }
        return true;
    }
}
