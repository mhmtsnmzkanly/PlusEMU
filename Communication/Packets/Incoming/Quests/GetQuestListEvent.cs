using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;

namespace Plus.Communication.Packets.Incoming.Quests;

public class GetQuestListEvent : IPacketEvent
{
    private readonly IQuestService _questService;

    public GetQuestListEvent(IQuestService questService)
    {
        _questService = questService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        await _questService.GetQuestList(session, false);
    }
}
