using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;

namespace Plus.Communication.Packets.Incoming.Quests;

internal class CancelQuestEvent : IPacketEvent
{
    private readonly IQuestService _questService;

    public CancelQuestEvent(IQuestService questService)
    {
        _questService = questService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        await _questService.CancelQuest(session);
    }
}
