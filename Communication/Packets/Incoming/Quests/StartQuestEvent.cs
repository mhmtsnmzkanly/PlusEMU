using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;

namespace Plus.Communication.Packets.Incoming.Quests;

internal class StartQuestEvent : IPacketEvent
{
    private readonly IQuestService _questService;

    public StartQuestEvent(IQuestService questService)
    {
        _questService = questService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var questId = packet.ReadInt();
        await _questService.StartQuest(session, questId);
    }
}
