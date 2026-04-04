using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Guides;

internal sealed class RequestGuideToolEvent : IPacketEvent
{
    private readonly IGuideService _guideService;

    public RequestGuideToolEvent(IGuideService guideService) => _guideService = guideService;

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        bool onDuty = packet.ReadBool();
        bool helperRequests = false;
        bool bullyReports = false;

        if (onDuty)
        {
            if (packet.HasDataRemaining())
                _ = packet.ReadBool();
            helperRequests = packet.HasDataRemaining() && packet.ReadBool();
            bullyReports = packet.HasDataRemaining() && packet.ReadBool();
        }

        return _guideService.ConfigureDuty(session, onDuty, helperRequests, bullyReports);
    }
}
