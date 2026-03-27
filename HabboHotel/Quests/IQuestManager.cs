using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Quests;

public interface IQuestManager
{
    void Init();
    Quest GetQuest(int id);
    bool TryGetQuest(int id, out Quest quest);
    int GetAmountOfQuestsInCategory(string category);
    Quest GetNextQuestInSeries(string category, int number);
    IReadOnlyDictionary<int, Quest> Quests { get; }
}