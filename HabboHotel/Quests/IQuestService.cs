using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Quests;

public interface IQuestService
{
    Task StartQuest(GameClient session, int questId);
    Task CancelQuest(GameClient session);
    Task GetCurrentQuest(GameClient session);
    Task GetQuestList(GameClient session, bool isFromEvent);
    Task ProgressUserQuest(GameClient session, QuestType type, int data = 0);
    Task QuestReminder(GameClient session, int questId);
}
