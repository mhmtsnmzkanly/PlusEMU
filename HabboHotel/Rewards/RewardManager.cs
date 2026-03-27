using System.Collections.Concurrent;
using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Database;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rewards;

public class RewardManager : IRewardManager
{
    private readonly IDatabase _database;
    private readonly IBadgeManager _badgeManager;
    private readonly ConcurrentDictionary<int, List<int>> _rewardLogs;
    private readonly ConcurrentDictionary<int, Reward> _rewards;

    public RewardManager(IDatabase database, IBadgeManager badgeManager)
    {
        _database = database;
        _badgeManager = badgeManager;
        _rewards = new();
        _rewardLogs = new();
    }

    public void Init()
    {
        using var db = _database.Connection();
        var rewardRows = db.Query(
            "SELECT `id`, `reward_start`, `reward_end`, `reward_type`, `reward_data`, `message` FROM `server_rewards` WHERE `enabled` = '1'");
        foreach (var row in rewardRows)
        {
            _rewards.TryAdd(
                (int)row.id,
                new(
                    Convert.ToDouble(row.reward_start),
                    Convert.ToDouble(row.reward_end),
                    ((string?)row.reward_type) ?? string.Empty,
                    ((string?)row.reward_data) ?? string.Empty,
                    ((string?)row.message) ?? string.Empty));
        }

        var logRows = db.Query("SELECT `user_id`, `reward_id` FROM `server_reward_logs`");
        foreach (var row in logRows)
        {
            var id = (int)row.user_id;
            var rewardId = (int)row.reward_id;
            var rewardLog = _rewardLogs.GetOrAdd(id, static _ => []);
            if (!rewardLog.Contains(rewardId))
                rewardLog.Add(rewardId);
        }
    }

    private bool HasReward(int id, int rewardId)
    {
        return _rewardLogs.TryGetValue(id, out var rewardLog) && rewardLog.Contains(rewardId);
    }

    private void LogReward(int id, int rewardId)
    {
        var rewardLog = _rewardLogs.GetOrAdd(id, static _ => []);
        if (!rewardLog.Contains(rewardId))
            rewardLog.Add(rewardId);

        using var db = _database.Connection();
        db.Execute(
            "INSERT INTO `server_reward_logs` VALUES ('', @userId, @rewardId)",
            new { userId = id, rewardId });
    }

    public async Task CheckRewards(GameClient session)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Inventory?.Badges == null)
            return;
        foreach (var entry in _rewards)
        {
            var id = entry.Key;
            var reward = entry.Value;
            if (HasReward(habbo.Id, id))
                continue;
            if (reward.Active)
            {
                var inventory = habbo.Inventory;
                if (inventory == null)
                    continue;
                switch (reward.Type)
                {
                    case RewardType.Badge:
                    {
                        if (!inventory.Badges.HasBadge(reward.RewardData))
                            await _badgeManager.GiveBadge(habbo, reward.RewardData);
                        break;
                    }
                    case RewardType.Credits:
                    {
                        habbo.Credits += Convert.ToInt32(reward.RewardData);
                        session.Send(new CreditBalanceComposer(habbo.Credits));
                        break;
                    }
                    case RewardType.Duckets:
                    {
                        habbo.Duckets += Convert.ToInt32(reward.RewardData);
                        session.Send(new HabboActivityPointNotificationComposer(habbo.Duckets, Convert.ToInt32(reward.RewardData)));
                        break;
                    }
                    case RewardType.Diamonds:
                    {
                        habbo.Diamonds += Convert.ToInt32(reward.RewardData);
                        session.Send(new HabboActivityPointNotificationComposer(habbo.Diamonds, Convert.ToInt32(reward.RewardData), 5));
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(reward.Message))
                    session.SendNotification(reward.Message);
                LogReward(habbo.Id, id);
            }
            else
                continue;
        }
    }
}
