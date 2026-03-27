using Dapper;
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
        if (habbo?.HabboStats == null) return Task.CompletedTask;
        var quest = _questManager.GetQuest(habbo.HabboStats.QuestId);
        if (quest == null) return Task.CompletedTask;
        using var db = _database.Connection();
        db.Execute("DELETE FROM `user_quests` WHERE `user_id` = @userId AND `quest_id` = @questId", new { userId = habbo.Id, questId = quest.Id });
        db.Execute("UPDATE `user_statistics` SET `quest_id` = '0' WHERE `id` = @id LIMIT 1", new { id = habbo.Id });
        habbo.HabboStats.QuestId = 0;
        session.Send(new QuestAbortedComposer());
        _questManager.GetList(session, null!);
        return Task.CompletedTask;
    }
}
