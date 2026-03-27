using Plus.HabboHotel.Badges;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class RoomBadgeCommand : IChatCommand
{
    private readonly IBadgeManager _badgeManager;
    public string Key => "roombadge";
    public string PermissionRequired => "command_room_badge";

    public string Parameters => "%badge%";

    public string Description => "Give a badge to the entire room!";

    public RoomBadgeCommand(IBadgeManager badgeManager)
    {
        _badgeManager = badgeManager;
    }

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        var badgeCode = parameters.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(badgeCode))
        {
            session.SendWhisper("Please enter the name of the badge you'd like to give to the room.");
            return;
        }
        foreach (var user in room.GetRoomUserManager().GetUserList().ToList())
        {
            var targetClient = user?.GetClient();
            var targetHabbo = targetClient?.GetHabbo();
            var targetBadges = targetHabbo?.Inventory?.Badges;
            if (targetHabbo == null || targetBadges == null || targetClient == null)
                continue;
            if (!targetBadges.HasBadge(badgeCode))
            {
                _badgeManager.GiveBadge(targetHabbo, badgeCode).Wait();
                targetClient.SendNotification("You have just been given a badge!");
            }
            else
                targetClient.SendWhisper($"{habbo.Username} tried to give you a badge, but you already have it!");
        }
        session.SendWhisper($"You have successfully given every user in this room the {badgeCode} badge!");
    }
}
