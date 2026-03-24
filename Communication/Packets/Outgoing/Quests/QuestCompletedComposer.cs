using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;

namespace Plus.Communication.Packets.Outgoing.Quests;

public class QuestCompletedComposer : IServerPacket
{
    private readonly GameClient _session;
    private readonly Quest _quest;

    public uint MessageId => ServerPacketHeader.QuestCompletedComposer;

    public QuestCompletedComposer(GameClient session, Quest quest)
    {
        _session = session;
        _quest = quest;
    }

    public void Compose(IOutgoingPacket packet)
    {
        var habbo = _session.GetHabbo();
        if (habbo?.HabboStats == null || _quest == null)
            return;

        var amountInCat = PlusEnvironment.Game.QuestManager.GetAmountOfQuestsInCategory(_quest.Category);
        var number = _quest.Number;
        var userProgress = habbo.GetQuestProgress(_quest.Id);
        packet.WriteString(_quest.Category);
        packet.WriteInteger(number); // Quest progress in this cat
        packet.WriteInteger(_quest.Name.Contains("xmas2012") ? 1 : amountInCat); // Total quests in this cat
        packet.WriteInteger(_quest.RewardType); // Reward type (1 = Snowflakes, 2 = Love hearts, 3 = Pixels, 4 = Seashells, everything else is pixels
        packet.WriteInteger(_quest.Id); // Quest id
        packet.WriteBoolean(habbo.HabboStats.QuestId == _quest.Id); // Quest started
        packet.WriteString(_quest.ActionName);
        packet.WriteString(_quest.DataBit);
        packet.WriteInteger(_quest.Reward);
        packet.WriteString(_quest.Name);
        packet.WriteInteger(userProgress); // Current progress
        packet.WriteInteger(_quest.GoalData); // Target progress
        packet.WriteInteger(_quest.TimeUnlock); // "Next quest available countdown" in seconds
        packet.WriteString("");
        packet.WriteString("");
        packet.WriteBoolean(true); // ?
        packet.WriteBoolean(true); // Activate next quest..
    }
}
