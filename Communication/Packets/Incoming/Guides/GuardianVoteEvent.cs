using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Guides;

internal sealed class GuardianVoteEvent : IPacketEvent
{
    private readonly IGuardianService _guardianService;

    public GuardianVoteEvent(IGuardianService guardianService) => _guardianService = guardianService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _guardianService.Vote(session, packet.ReadInt());
}
