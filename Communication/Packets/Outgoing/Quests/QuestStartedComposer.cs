using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;

namespace Plus.Communication.Packets.Outgoing.Quests;

public class QuestStartedComposer : IServerPacket
{
    private readonly GameClient _session;
    private readonly Quest _quest;
    private readonly IQuestManager _questManager;
    public uint MessageId => ServerPacketHeader.QuestStartedComposer;

    public QuestStartedComposer(GameClient session, Quest quest, IQuestManager questManager)
    {
        _session = session;
        _quest = quest;
        _questManager = questManager;
    }

    public void Compose(IOutgoingPacket packet)
    {
        SerializeQuest(packet, _session, _quest);
    }

    private void SerializeQuest(IOutgoingPacket packet, GameClient session, Quest quest)
    {
        var habbo = session?.GetHabbo();
        if (packet == null || session == null || quest == null || habbo?.HabboStats == null)
            return;
        var amountInCat = _questManager.GetAmountOfQuestsInCategory(quest.Category);
        var number = quest.Number - 1;
        var userProgress = habbo.GetQuestProgress(quest.Id);
        if (quest.IsCompleted(userProgress))
            number++;
        packet.WriteString(quest.Category);
        packet.WriteInteger(quest.Category.Contains("xmas2012") ? 0 : number); // Quest progress in this cat
        packet.WriteInteger(quest.Category.Contains("xmas2012") ? 0 : amountInCat); // Total quests in this cat
        packet.WriteInteger(quest.RewardType); // Reward type (1 = Snowflakes, 2 = Love hearts, 3 = Pixels, 4 = Seashells, everything else is pixels
        packet.WriteInteger(quest.Id); // Quest id
        packet.WriteBoolean(habbo.HabboStats.QuestId == quest.Id); // Quest started
        packet.WriteString(quest.ActionName);
        packet.WriteString(quest.DataBit);
        packet.WriteInteger(quest.Reward);
        packet.WriteString(quest.Name);
        packet.WriteInteger(userProgress); // Current progress
        packet.WriteInteger(quest.GoalData); // Target progress
        packet.WriteInteger(quest.TimeUnlock); // "Next quest available countdown" in seconds
        packet.WriteString("");
        packet.WriteString("");
        packet.WriteBoolean(true);
    }
}
