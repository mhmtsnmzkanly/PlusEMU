using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Guides;

internal sealed class RequestGuideAssistanceEvent : IPacketEvent
{
    private readonly IGuideService _guideService;

    public RequestGuideAssistanceEvent(IGuideService guideService) => _guideService = guideService;

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        int requestType = packet.ReadInt();
        string message = packet.ReadString();
        return _guideService.RequestAssistance(session, requestType, message);
    }
}
