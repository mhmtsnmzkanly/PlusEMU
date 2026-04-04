using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Guides;

internal sealed class GuardianNoUpdatesWantedEvent : IPacketEvent
{
    private readonly IGuardianService _guardianService;

    public GuardianNoUpdatesWantedEvent(IGuardianService guardianService) => _guardianService = guardianService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _guardianService.IgnoreUpdates(session);
}
