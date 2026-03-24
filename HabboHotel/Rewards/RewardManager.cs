using System.Collections.Concurrent;
using System.Data;
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
        using var dbClient = _database.GetQueryReactor();
        dbClient.SetQuery("SELECT * FROM `server_rewards` WHERE enabled = '1'");
        var dTable = dbClient.GetTable();
        if (dTable != null)
        {
            foreach (DataRow dRow in dTable.Rows)
            {
                _rewards.TryAdd((int)dRow["id"],
                    new(
                        Convert.ToDouble(dRow["reward_start"]),
                        Convert.ToDouble(dRow["reward_end"]),
                        Convert.ToString(dRow["reward_type"]) ?? string.Empty,
                        Convert.ToString(dRow["reward_data"]) ?? string.Empty,
                        Convert.ToString(dRow["message"]) ?? string.Empty));
            }
        }
        dbClient.SetQuery("SELECT * FROM `server_reward_logs`");
        dTable = dbClient.GetTable();
        if (dTable != null)
        {
            foreach (DataRow dRow in dTable.Rows)
            {
                var id = (int)dRow["user_id"];
                var rewardId = (int)dRow["reward_id"];
                var rewardLog = _rewardLogs.GetOrAdd(id, static _ => []);
                if (!rewardLog.Contains(rewardId))
                    rewardLog.Add(rewardId);
            }
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

        using var dbClient = _database.GetQueryReactor();
        dbClient.SetQuery("INSERT INTO `server_reward_logs` VALUES ('', @userId, @rewardId)");
        dbClient.AddParameter("userId", id);
        dbClient.AddParameter("rewardId", rewardId);
        dbClient.RunQuery();
    }

    public async Task CheckRewards(GameClient session)
    {
        var habbo = session?.GetHabbo();
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
                switch (reward.Type)
                {
                    case RewardType.Badge:
                    {
                        if (!habbo.Inventory.Badges.HasBadge(reward.RewardData))
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
