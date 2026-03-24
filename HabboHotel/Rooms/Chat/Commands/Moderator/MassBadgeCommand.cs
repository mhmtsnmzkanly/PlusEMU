using Plus.HabboHotel.Badges;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class MassBadgeCommand : IChatCommand
{
    private readonly IGameClientManager _gameClientManager;
    private readonly IBadgeManager _badgeManager;
    public string Key => "massbadge";
    public string PermissionRequired => "command_mass_badge";

    public string Parameters => "%badge%";

    public string Description => "Give a badge to the entire hotel.";

    public MassBadgeCommand(IGameClientManager gameClientManager, IBadgeManager badgeManager)
    {
        _gameClientManager = gameClientManager;
        _badgeManager = badgeManager;
    }

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        var username = habbo?.Username;
        if (string.IsNullOrEmpty(username))
            return;

        var badgeCode = parameters.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(badgeCode))
        {
            session.SendWhisper("Please enter the code of the badge you'd like to give to the entire hotel.");
            return;
        }
        foreach (var client in _gameClientManager.GetClients.ToList())
        {
            var targetHabbo = client?.GetHabbo();
            if (targetHabbo == null || targetHabbo.Username == username)
                continue;
            if (!targetHabbo.Inventory.Badges.HasBadge(badgeCode))
            {
                _badgeManager.GiveBadge(targetHabbo, badgeCode).Wait();
                client.SendNotification("You have just been given a badge!");
            }
            else
                client.SendWhisper($"{username} tried to give you a badge, but you already have it!");
        }
        session.SendWhisper($"You have successfully given every user in this hotel the {badgeCode} badge!");
    }
}
