using Plus.Communication.Packets.Outgoing.Help;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Help;

internal class GetSanctionStatusEvent : IPacketEvent
{
    private readonly ISanctionStatusService _sanctionStatusService;

    public GetSanctionStatusEvent(ISanctionStatusService sanctionStatusService)
    {
        _sanctionStatusService = sanctionStatusService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet) =>
        session.Send(new SanctionStatusComposer(await _sanctionStatusService.GetStatus(session)));
}
