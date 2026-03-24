using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Navigator;

namespace Plus.Communication.Packets.Incoming.Navigator;

internal class UpdateNavigatorSettingsEvent : IPacketEvent
{
    private readonly INavigatorService _navigatorService;

    public UpdateNavigatorSettingsEvent(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _navigatorService.UpdateSettings(session, packet.ReadUInt());
}
