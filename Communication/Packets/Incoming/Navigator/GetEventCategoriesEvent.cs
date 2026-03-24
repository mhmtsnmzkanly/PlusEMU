using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Navigator;

namespace Plus.Communication.Packets.Incoming.Navigator;

internal class GetEventCategoriesEvent : IPacketEvent
{
    private readonly INavigatorService _navigatorService;

    public GetEventCategoriesEvent(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _navigatorService.GetEventCategories(session);
}
