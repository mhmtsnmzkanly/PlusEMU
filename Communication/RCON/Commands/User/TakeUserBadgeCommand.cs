using Plus.HabboHotel.GameClients;

namespace Plus.Communication.RCON.Commands.User;

internal class TakeUserBadgeCommand : IRconCommand
{
    private readonly IGameClientManager _gameClientManager;
    public string Description => "This command is used to take a badge from a user.";

    public string Key => "take_user_badge";
    public string Parameters => "%userId% %badgeId%";

    public TakeUserBadgeCommand(IGameClientManager gameClientManager)
    {
        _gameClientManager = gameClientManager;
    }

    public Task<bool> TryExecute(string[] parameters)
    {
        if (!int.TryParse(parameters[0], out var userId))
            return Task.FromResult(false);
        var client = _gameClientManager.GetClientByUserId(userId);
        var habbo = client?.GetHabbo();
        if (habbo == null)
            return Task.FromResult(false);

        // Validate the badge
        var badge = Convert.ToString(parameters[1]);
        if (string.IsNullOrEmpty(badge))
            return Task.FromResult(false);
        if (habbo.Inventory.Badges.HasBadge(badge)) habbo.Inventory.Badges.RemoveBadge(badge);
        return Task.FromResult(true);
    }
}
