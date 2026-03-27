using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Achievements;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Database;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Messenger;

namespace Plus.HabboHotel.Achievements;

public class AchievementService : IAchievementService
{
    private readonly IAchievementManager _achievementManager;
    private readonly IDatabase _database;
    private readonly IBadgeManager _badgeManager;

    public AchievementService(IAchievementManager achievementManager, IDatabase database, IBadgeManager badgeManager)
    {
        _achievementManager = achievementManager;
        _database = database;
        _badgeManager = badgeManager;
    }

    public async Task<bool> ProgressAchievement(GameClient session, string group, int progress, bool fromBeginning = false)
    {
        if (!_achievementManager.Achievements.ContainsKey(group) || session == null)
            return false;
        var data = _achievementManager.Achievements[group];
        if (data == null) return false;
        var habbo = session.GetHabbo();
        if (habbo == null)
            return false;
        var userData = habbo.GetAchievementData(group);
        if (userData == null)
        {
            userData = new(group, 0, 0);
            habbo.Achievements.TryAdd(group, userData);
        }
        var totalLevels = data.Levels.Count;
        if (userData.Level == totalLevels)
            return false; // done, no more.
        var targetLevel = userData.Level + 1;
        if (targetLevel > totalLevels)
            targetLevel = totalLevels;
        var level = data.Levels[targetLevel];
        int newProgress;
        if (fromBeginning)
            newProgress = progress;
        else
            newProgress = userData.Progress + progress;
        var newLevel = userData.Level;
        var newTarget = newLevel + 1;
        if (newTarget > totalLevels)
            newTarget = totalLevels;
        if (newProgress >= level.Requirement)
        {
            newLevel++;
            newTarget++;
            newProgress = 0;
            var badges = habbo.Inventory?.Badges;
            if (targetLevel != 1 && badges != null)
                badges.RemoveBadge(group + (targetLevel - 1));
            await _badgeManager.GiveBadge(habbo, group + targetLevel);
            if (newTarget > totalLevels) newTarget = totalLevels;
            session.Send(new AchievementUnlockedComposer(data, targetLevel, level.RewardPoints, level.RewardPixels));

            using (var connection = _database.Connection())
            {
                await connection.ExecuteAsync("REPLACE INTO `user_achievements` VALUES (@habboId, @group, @newLevel, @newProgress)",
                    new { habboId = habbo.Id, group, newLevel, newProgress });
            }

            userData.Level = newLevel;
            userData.Progress = newProgress;
            habbo.Duckets += level.RewardPixels;
            habbo.HabboStats.AchievementPoints += level.RewardPoints;
            session.Send(new HabboActivityPointNotificationComposer(habbo.Duckets, level.RewardPixels));
            session.Send(new AchievementScoreComposer(habbo.HabboStats.AchievementPoints));
            var newLevelData = data.Levels[newTarget];
            session.Send(new AchievementProgressedComposer(data, newTarget, newLevelData, totalLevels, userData));
            return true;
        }
        userData.Level = newLevel;
        userData.Progress = newProgress;

        using (var connection = _database.Connection())
        {
            await connection.ExecuteAsync("REPLACE INTO `user_achievements` VALUES (@habboId, @group, @newLevel, @newProgress)",
                new { habboId = habbo.Id, group, newLevel, newProgress });
        }

        session.Send(new AchievementProgressedComposer(data, targetLevel, level, totalLevels, userData));
        return false;
    }
}
