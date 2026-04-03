using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;

namespace Plus.HabboHotel.Quests;

public class QuestManager : IQuestManager
{
    private readonly IDatabase _database;
    private readonly ILogger<QuestManager> _logger;
    private readonly Dictionary<string, int> _questCount;
    private readonly Dictionary<int, Quest> _quests;

    public QuestManager(IDatabase database, ILogger<QuestManager> logger)
    {
        _database = database;
        _logger = logger;
        _quests = new();
        _questCount = new();
    }

    public IReadOnlyDictionary<int, Quest> Quests => _quests;

    public void Init()
    {
        if (_quests.Count > 0)
            _quests.Clear();
        using var db = _database.Connection();
        var rows = db.Query(
            "SELECT `id`, `type`, `level_num`, `goal_type`, `goal_data`, `action`, `pixel_reward`, `data_bit`, `reward_type`, `timestamp_unlock`, `timestamp_lock` FROM `quests`")
            .ToList();
        foreach (var row in rows)
        {
            var id = (int)row.id;
            var category = ((string?)row.type) ?? string.Empty;
            _quests.Add(id, new(id, category, (int)row.level_num, (QuestType)(int)row.goal_type, (int)row.goal_data, 
                ((string?)row.action) ?? string.Empty, (int)row.pixel_reward, ((string?)row.data_bit) ?? string.Empty, 
                Convert.ToInt32(((object)row.reward_type).ToString()), (int)row.timestamp_unlock, (int)row.timestamp_lock));
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

    public bool TryGetQuest(int id, out Quest quest) => _quests.TryGetValue(id, out quest!);

    public int GetAmountOfQuestsInCategory(string category)
    {
        _questCount.TryGetValue(category, out var count);
        return count;
    }

    public Quest? GetNextQuestInSeries(string category, int number)
    {
        foreach (var quest in _quests.Values)
            if (quest.Category == category && quest.Number == number)
                return quest;
        return null;
    }
}
