using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Communication.Packets.Outgoing.Inventory.Badges;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Badges;

namespace Plus.HabboHotel.Badges;

public class BadgeManager : IBadgeManager
{
    private readonly IDatabase _database;
    private readonly GameClientManager _gameClientManager;
    private readonly ILogger<BadgeManager> _logger;

    private Dictionary<string, BadgeDefinition> _badges;
    public IReadOnlyDictionary<string, BadgeDefinition> Badges => _badges;

    public BadgeManager(IDatabase database, GameClientManager gameClientManager, ILogger<BadgeManager> logger)
    {
        _database = database;
        _gameClientManager = gameClientManager;
        _logger = logger;
        _badges = new Dictionary<string, BadgeDefinition>();
    }

    public async Task Init()
    {
        using var connection = _database.Connection();
        _badges = (await connection.QueryAsync<BadgeDefinition>("SELECT * FROM badge_definitions")).ToDictionary(definition => definition.Code.ToUpper());
        _logger.LogInformation("Loaded " + Badges.Count + " badge definitions.");
    }

    public async Task GiveBadge(Habbo habbo, string code)
    {
        var badges = habbo.Inventory?.Badges;
        if (badges == null || !habbo.TryGetClient(out var client))
            return;

        if (badges.HasBadge(code))
            return;

        if (!_badges.TryGetValue(code.ToUpper(), out var badge))
            return;

        var requiredRight = badge.RequiredRight ?? string.Empty;
        if (requiredRight.Length > 0 && !(habbo.Permissions?.HasRight(requiredRight) ?? false))
            return;

        using var connection = _database.Connection();
        await connection.ExecuteScalarAsync<int>("REPLACE INTO `user_badges` (`user_id`,`badge_id`,`badge_slot`) VALUES (@userId, @badge, '0');", new
        {
            userId = habbo.Id,
            badge = badge.Code
        });
        badges.AddBadge(new Badge(code, 0));

        client.Send(new BadgesComposer(habbo.Id, badges.Badges));
        client.Send(new FurniListNotificationComposer(1, 4));
    }

    public async Task RemoveBadge(Habbo habbo, string badge)
    {
        var badges = habbo.Inventory?.Badges;
        if (badges == null || !badges.HasBadge(badge)) return;
        using var connection = _database.Connection();
        await connection.ExecuteAsync("DELETE FROM user_badges WHERE badge_id = @badge AND user_id = @userId LIMIT 1", new
        {
            badge,
            userId = habbo.Id
        });
        badges.RemoveBadge(badge);
    }

    public async Task<List<Badge>> LoadBadgesForHabbo(int userId)
    {
        using var connection = _database.Connection();
        return (await connection.QueryAsync<Badge>("SELECT badge_id as code, badge_slot as slot FROM user_badges WHERE user_id = @userId", new { userId })).ToList();
    }

    public async Task<List<Badge>> GetEquippedBadgesForUserAsync(int userId)
    {
        var gameClient = _gameClientManager.GetClientByUserId(userId);
        var habbo = gameClient?.GetHabbo();

        if (habbo is { Inventory.Badges: not null })
        {
            return habbo.Inventory.Badges.EquippedBadges;
        }

        using var connection = _database.Connection();
        return (await connection.QueryAsync<Badge>("SELECT badge_id as code, badge_slot as slot FROM user_badges WHERE user_id = @userId AND badge_slot > 0", new { userId })).ToList();
    }

    public async Task UpdateUserBadges(Habbo habbo, List<(int slot, string badge)> badgeUpdates)
    {
        if (habbo?.Inventory?.Badges == null)
        {
            _logger.LogWarning("Attempted to update badges for a user with null inventory or badges collection.");
            return;
        }

        habbo.Inventory.Badges.ClearWearingBadges();

        foreach (var (slot, badgeCode) in badgeUpdates)
        {
            var badge = habbo.Inventory.Badges.GetBadge(badgeCode);
            if (badge != null)
            {
                badge.Slot = slot;
            }
            else
            {
                _logger.LogWarning($"Badge {badgeCode} not found for user {habbo.Id} during update.");
            }
        }

        using var connection = _database.Connection();
        var userIdParam = new { userId = habbo.Id };
        await connection.ExecuteAsync("UPDATE `user_badges` SET `badge_slot` = '0' WHERE `user_id` = @userId", userIdParam);

        foreach (var (slot, badge) in badgeUpdates)
        {
            await connection.ExecuteAsync(
                "UPDATE `user_badges` SET `badge_slot` = @Slot WHERE `badge_id` = @Badge AND `user_id` = @userId LIMIT 1",
                new { slot, Badge = badge, userId = habbo.Id });
        }
    }
}
