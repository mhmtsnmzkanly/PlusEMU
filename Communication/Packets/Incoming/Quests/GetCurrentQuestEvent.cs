using Dapper;
using Plus.Communication.Packets.Outgoing.Quests;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;

namespace Plus.Communication.Packets.Incoming.Quests;

internal class GetCurrentQuestEvent : IPacketEvent
{
    private readonly IQuestManager _questManager;
    private readonly IDatabase _database;

    public GetCurrentQuestEvent(IQuestManager questManager, IDatabase database)
    {
        _questManager = questManager;
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null || !habbo.InRoom)
            return Task.CompletedTask;
        var userQuest = _questManager.GetQuest(habbo.QuestLastCompleted);
        if (userQuest == null)
            return Task.CompletedTask;
        var nextQuest = _questManager.GetNextQuestInSeries(userQuest.Category, userQuest.Number + 1);
        if (nextQuest == null)
            return Task.CompletedTask;
        using var db = _database.Connection();
        db.Execute("REPLACE INTO `user_quests` (`user_id`, `quest_id`) VALUES (@userId, @questId)",
            new { userId = habbo.Id, questId = nextQuest.Id });
        db.Execute("UPDATE `user_statistics` SET `quest_id` = @questId WHERE `id` = @id LIMIT 1",
            new { questId = nextQuest.Id, id = habbo.Id });
        habbo.HabboStats.QuestId = nextQuest.Id;
        _questManager.GetList(session, null!);
        session.Send(new QuestStartedComposer(session, nextQuest));
        return Task.CompletedTask;
    }
}
