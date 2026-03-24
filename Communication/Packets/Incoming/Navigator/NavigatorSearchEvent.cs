using Plus.Communication.Packets.Outgoing.Navigator.New;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Navigator;

namespace Plus.Communication.Packets.Incoming.Navigator;

internal class NavigatorSearchEvent : IPacketEvent
{
    private readonly INavigatorService _navigatorService;

    public NavigatorSearchEvent(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var category = packet.ReadString();
        var search = packet.ReadString();
        return _navigatorService.Search(session, category, search);
    }
}
