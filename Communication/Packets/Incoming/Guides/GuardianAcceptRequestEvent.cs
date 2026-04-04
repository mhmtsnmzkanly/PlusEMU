using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Guides;

internal sealed class GuardianAcceptRequestEvent : IPacketEvent
{
    private readonly IGuardianService _guardianService;

    public GuardianAcceptRequestEvent(IGuardianService guardianService) => _guardianService = guardianService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _guardianService.AcceptTicket(session, packet.ReadBool());
}
