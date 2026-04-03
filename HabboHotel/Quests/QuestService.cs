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

internal class QuestService : IQuestService
{
    private readonly IDatabase _database;
    private readonly IQuestManager _questManager;
    private readonly IMessengerDataLoader _messengerDataLoader;
    private readonly ILogger<QuestService> _logger;

    public QuestService(IDatabase database, IQuestManager questManager, IMessengerDataLoader messengerDataLoader, ILogger<QuestService> logger)
    {
        _database = database;
        _questManager = questManager;
        _messengerDataLoader = messengerDataLoader;
        _logger = logger;
    }

    public async Task GetQuestList(GameClient session, bool isFromEvent)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null) return;

        var userQuestGoals = new Dictionary<string, int>();
        var userQuests = new Dictionary<string, Quest>();
        
        foreach (var quest in _questManager.Quests.Values.ToList())
        {
            if (quest.Category.Contains("xmas2012")) continue;
            
            if (!userQuestGoals.ContainsKey(quest.Category))
            {
                userQuestGoals.Add(quest.Category, 1);
                userQuests.Add(quest.Category, null!);
            }
            
            if (quest.Number >= userQuestGoals[quest.Category])
            {
                var userProgress = habbo.GetQuestProgress(quest.Id);
                if (habbo.HabboStats.QuestId != quest.Id && userProgress >= quest.GoalData) 
                    userQuestGoals[quest.Category] = quest.Number + 1;
            }
        }
        
        foreach (var quest in _questManager.Quests.Values.ToList())
        {
            foreach (var goal in userQuestGoals)
            {
                if (quest.Category.Contains("xmas2012")) continue;
                if (quest.Category == goal.Key && quest.Number == goal.Value)
                {
                    userQuests[goal.Key] = quest;
                    break;
                }
            }
        }
        
        session.Send(new QuestListComposer(session, isFromEvent, userQuests!, _questManager));
    }

    public async Task GetCurrentQuest(GameClient session)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null) return;

        var lastQuest = _questManager.GetQuest(habbo.QuestLastCompleted);
        if (lastQuest == null) return;

        var nextQuest = _questManager.GetNextQuestInSeries(lastQuest.Category, lastQuest.Number + 1);
        if (nextQuest == null) return;

        using var connection = _database.Connection();
        connection.Execute("REPLACE INTO `user_quests` (`user_id`, `quest_id`) VALUES (@userId, @questId)",
            new { userId = habbo.Id, questId = nextQuest.Id });
        connection.Execute("UPDATE `user_statistics` SET `quest_id` = @questId WHERE `id` = @id LIMIT 1",
            new { questId = nextQuest.Id, id = habbo.Id });

        habbo.HabboStats.QuestId = nextQuest.Id;
        await GetQuestList(session, false);
        session.Send(new QuestStartedComposer(session, nextQuest, _questManager));
    }

    public async Task StartQuest(GameClient session, int questId)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null) return;

        var quest = _questManager.GetQuest(questId);
        if (quest == null) return;

        using var connection = _database.Connection();
        connection.Execute("REPLACE INTO `user_quests` (`user_id`, `quest_id`) VALUES (@userId, @questId)", new { userId = habbo.Id, questId = quest.Id });
        connection.Execute("UPDATE `user_statistics` SET `quest_id` = @questId WHERE `id` = @id LIMIT 1", new { questId = quest.Id, id = habbo.Id });

        habbo.HabboStats.QuestId = quest.Id;
        await GetQuestList(session, false);
        session.Send(new QuestStartedComposer(session, quest, _questManager));
    }

    public async Task CancelQuest(GameClient session)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null || habbo.HabboStats.QuestId == 0) return;

        using var connection = _database.Connection();
        connection.Execute("UPDATE `user_statistics` SET `quest_id` = '0' WHERE `id` = @id LIMIT 1", new { id = habbo.Id });
        
        habbo.HabboStats.QuestId = 0;
        session.Send(new QuestAbortedComposer());
        await GetQuestList(session, false);
    }

    public Task QuestReminder(GameClient session, int questId)
    {
        if (_questManager.TryGetQuest(questId, out var quest) && quest != null)
            session.Send(new QuestStartedComposer(session, quest, _questManager));
        return Task.CompletedTask;
    }

    public async Task ProgressUserQuest(GameClient session, QuestType type, int data = 0)
    {
        var habbo = session.GetHabbo();
        var stats = habbo?.HabboStats;
        var quests = habbo?.Quests;
        if (habbo == null || stats == null || quests == null || stats.QuestId <= 0) return;

        var quest = _questManager.GetQuest(stats.QuestId);
        if (quest == null || quest.GoalType != type) return;

        var totalProgress = habbo.GetQuestProgress(quest.Id);
        var completeQuest = false;

        switch (type)
        {
            default:
                totalProgress++;
                if (totalProgress >= quest.GoalData) completeQuest = true;
                break;
            case QuestType.ExploreFindItem:
            case QuestType.StandOn:
            case QuestType.GiveItem:
                if (data != quest.GoalData) return;
                totalProgress = quest.GoalData;
                completeQuest = true;
                break;
            case QuestType.XmasParty:
                totalProgress++;
                if (totalProgress == quest.GoalData) completeQuest = true;
                break;
        }

        using var connection = _database.Connection();
        connection.Execute("UPDATE `user_quests` SET `progress` = @progress WHERE `user_id` = @userId AND `quest_id` = @questId LIMIT 1",
            new { progress = totalProgress, userId = habbo.Id, questId = quest.Id });

        if (completeQuest)
            connection.Execute("UPDATE `user_statistics` SET `quest_id` = '0' WHERE `id` = @id LIMIT 1", new { id = habbo.Id });

        quests[stats.QuestId] = totalProgress;
        session.Send(new QuestStartedComposer(session, quest, _questManager));

        if (completeQuest)
        {
            _messengerDataLoader.BroadcastStatusUpdate(habbo, MessengerEventTypes.QuestCompleted, $"{quest.Category}.{quest.Name}");
            stats.QuestId = 0;
            habbo.QuestLastCompleted = quest.Id;
            session.Send(new QuestCompletedComposer(session, quest, _questManager));
            habbo.Duckets += quest.Reward;
            session.Send(new HabboActivityPointNotificationComposer(habbo.Duckets, quest.Reward));
            await GetQuestList(session, false);
        }
    }
}
