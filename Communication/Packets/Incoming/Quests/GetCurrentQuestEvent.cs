using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;

namespace Plus.Communication.Packets.Incoming.Quests;

internal class GetCurrentQuestEvent : IPacketEvent
{
    private readonly IQuestService _questService;

    public GetCurrentQuestEvent(IQuestService questService)
    {
        _questService = questService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        await _questService.GetCurrentQuest(session);
    }
}
