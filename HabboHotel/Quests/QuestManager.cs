using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Communication.Packets.Incoming;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Communication.Packets.Outgoing.Quests;
using Plus.Database;
using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Messenger;

namespace Plus.HabboHotel.Quests;

public class QuestManager : IQuestManager
{
    private readonly IDatabase _database;
    private readonly IMessengerDataLoader _messengerDataLoader;
    private readonly ILogger<QuestManager> _logger;
    private readonly Dictionary<string, int> _questCount;

    private readonly Dictionary<int, Quest> _quests;

    public QuestManager(IDatabase database, IMessengerDataLoader messengerDataLoader, ILogger<QuestManager> logger)
    {
        _database = database;
        _messengerDataLoader = messengerDataLoader;
        _logger = logger;
        _quests = new();
        _questCount = new();
    }

    public void Init()
    {
        if (_quests.Count > 0)
            _quests.Clear();
        using var db = _database.Connection();
        var rows = db.Query(
            "SELECT `id`, `type`, `level_num`, `goal_type`, `goal_data`, `action`, `pixel_reward`, `data_bit`, `reward_type`, `timestamp_unlock`, `timestamp_lock` FROM `quests`");
        foreach (var row in rows)
        {
            var id = (int)row.id;
            var category = ((string?)row.type) ?? string.Empty;
            var num = (int)row.level_num;
            var type = (int)row.goal_type;
            var goalData = (int)row.goal_data;
            var name = ((string?)row.action) ?? string.Empty;
            var reward = (int)row.pixel_reward;
            var dataBit = ((string?)row.data_bit) ?? string.Empty;
            var rewardtype = Convert.ToInt32(((object)row.reward_type).ToString());
            var time = (int)row.timestamp_unlock;
            var locked = (int)row.timestamp_lock;
            _quests.Add(id, new(id, category, num, (QuestType)type, goalData, name, reward, dataBit, rewardtype, time, locked));
            AddToCounter(category);
        }
        _logger.LogInformation("Quest Manager -> LOADED");
    }

    private void AddToCounter(string category)
    {
        if (_questCount.TryGetValue(category, out var count))
            _questCount[category] = count + 1;
        else
            _questCount.Add(category, 1);
    }

    public Quest GetQuest(int id)
    {
        _quests.TryGetValue(id, out var quest);
        return quest!;
    }

    public int GetAmountOfQuestsInCategory(string category)
    {
        _questCount.TryGetValue(category, out var count);
        return count;
    }

    public void ProgressUserQuest(GameClient session, QuestType type, int data = 0)
    {
        var client = session;
        if (client == null)
            return;
        var habbo = client.GetHabbo();
        var stats = habbo?.HabboStats;
        var quests = habbo?.Quests;
        if (habbo == null || stats == null || quests == null || stats.QuestId <= 0) return;
        var quest = GetQuest(stats.QuestId);
        if (quest == null || quest.GoalType != type) return;
        var currentProgress = habbo.GetQuestProgress(quest.Id);
        var totalProgress = currentProgress;
        var completeQuest = false;
        switch (type)
        {
            default:
                totalProgress++;
                if (totalProgress >= quest.GoalData) completeQuest = true;
                break;
            case QuestType.ExploreFindItem:
                if (data != quest.GoalData)
                    return;
                totalProgress = Convert.ToInt32(quest.GoalData);
                completeQuest = true;
                break;
            case QuestType.StandOn:
                if (data != quest.GoalData)
                    return;
                totalProgress = Convert.ToInt32(quest.GoalData);
                completeQuest = true;
                break;
            case QuestType.XmasParty:
                totalProgress++;
                if (totalProgress == quest.GoalData)
                    completeQuest = true;
                break;
            case QuestType.GiveItem:
                if (data != quest.GoalData)
                    return;
                totalProgress = Convert.ToInt32(quest.GoalData);
                completeQuest = true;
                break;
        }
        using var db = _database.Connection();
        db.Execute(
            "UPDATE `user_quests` SET `progress` = @progress WHERE `user_id` = @userId AND `quest_id` = @questId LIMIT 1",
            new { progress = totalProgress, userId = habbo.Id, questId = quest.Id });
        if (completeQuest)
            db.Execute(
                "UPDATE `user_statistics` SET `quest_id` = '0' WHERE `id` = @id LIMIT 1",
                new { id = habbo.Id });
        quests[stats.QuestId] = totalProgress;
        var activeQuest = quest;
        if (activeQuest == null)
            return;
        client.Send(new QuestStartedComposer(client, activeQuest));
        if (completeQuest)
        {
            _messengerDataLoader.BroadcastStatusUpdate(habbo, MessengerEventTypes.QuestCompleted, $"{activeQuest.Category}.{activeQuest.Name}");
            stats.QuestId = 0;
            habbo.QuestLastCompleted = activeQuest.Id;
            client.Send(new QuestCompletedComposer(client, activeQuest));
            habbo.Duckets += activeQuest.Reward;
            client.Send(new HabboActivityPointNotificationComposer(habbo.Duckets, activeQuest.Reward));
            GetList(client, null!);
        }
    }

    public Quest GetNextQuestInSeries(string category, int number)
    {
        foreach (var quest in _quests.Values)
            if (quest.Category == category && quest.Number == number)
                return quest;
        return null!;
    }

    public void GetList(GameClient session, ClientPacket message)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null)
            return;

        var userQuestGoals = new Dictionary<string, int>();
        var userQuests = new Dictionary<string, Quest>();
        foreach (var quest in _quests.Values.ToList())
        {
            if (quest.Category.Contains("xmas2012"))
                continue;
            if (!userQuestGoals.ContainsKey(quest.Category))
            {
                userQuestGoals.Add(quest.Category, 1);
                userQuests.Add(quest.Category, null!);
            }
            if (quest.Number >= userQuestGoals[quest.Category])
            {
                var userProgress = habbo.GetQuestProgress(quest.Id);
                if (habbo.HabboStats.QuestId != quest.Id && userProgress >= quest.GoalData) userQuestGoals[quest.Category] = quest.Number + 1;
            }
        }
        foreach (var quest in _quests.Values.ToList())
        {
            foreach (var goal in userQuestGoals)
            {
                if (quest.Category.Contains("xmas2012"))
                    continue;
                if (quest.Category == goal.Key && quest.Number == goal.Value)
                {
                    userQuests[goal.Key] = quest;
                    break;
                }
            }
        }
        session.Send(new QuestListComposer(session, message != null, userQuests));
    }

    public void QuestReminder(GameClient session, int questId)
    {
        var quest = GetQuest(questId);
        if (quest == null)
            return;
        session.Send(new QuestStartedComposer(session, quest));
    }
}
