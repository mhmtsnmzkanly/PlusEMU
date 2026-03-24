using Plus.Communication.Packets.Outgoing.Quests;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;

namespace Plus.Communication.Packets.Incoming.Quests;

internal class CancelQuestEvent : IPacketEvent
{
    private readonly IQuestManager _questManager;
    private readonly IDatabase _database;

    public CancelQuestEvent(IQuestManager questManager, IDatabase database)
    {
        _questManager = questManager;
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null)
            return Task.CompletedTask;

        var quest = _questManager.GetQuest(habbo.HabboStats.QuestId);
        if (quest == null)
            return Task.CompletedTask;
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.RunQuery(
                $"DELETE FROM `user_quests` WHERE `user_id` = '{habbo.Id}' AND `quest_id` = '{quest.Id}';UPDATE `user_statistics` SET `quest_id` = '0' WHERE `id` = '{habbo.Id}' LIMIT 1");
        }
        habbo.HabboStats.QuestId = 0;
        session.Send(new QuestAbortedComposer());
        _questManager.GetList(session, null!);
        return Task.CompletedTask;
    }
}
