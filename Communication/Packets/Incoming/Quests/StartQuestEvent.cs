using Dapper;
using Plus.Communication.Packets.Outgoing.Quests;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;

namespace Plus.Communication.Packets.Incoming.Quests;

internal class StartQuestEvent : IPacketEvent
{
    private readonly IQuestManager _questManager;
    private readonly IDatabase _database;

    public StartQuestEvent(IQuestManager questManager, IDatabase database)
    {
        _questManager = questManager;
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null) return Task.CompletedTask;
        var questId = packet.ReadInt();
        var quest = _questManager.GetQuest(questId);
        if (quest == null) return Task.CompletedTask;
        using var db = _database.Connection();
        db.Execute("REPLACE INTO `user_quests` (`user_id`, `quest_id`) VALUES (@userId, @questId)", new { userId = habbo.Id, questId = quest.Id });
        db.Execute("UPDATE `user_statistics` SET `quest_id` = @questId WHERE `id` = @id LIMIT 1", new { questId = quest.Id, id = habbo.Id });
        habbo.HabboStats.QuestId = quest.Id;
        _questManager.GetList(session, null!);
        session.Send(new QuestStartedComposer(session, quest));
        return Task.CompletedTask;
    }
}
