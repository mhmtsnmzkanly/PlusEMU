using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Guides;

internal sealed class GuideInviteUserEvent : IPacketEvent
{
    private readonly IGuideService _guideService;

    public GuideInviteUserEvent(IGuideService guideService) => _guideService = guideService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _guideService.InviteRequesterToRoom(session);
}
