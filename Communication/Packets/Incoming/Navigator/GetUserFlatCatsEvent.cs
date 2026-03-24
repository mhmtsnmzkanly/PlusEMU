using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Navigator;

namespace Plus.Communication.Packets.Incoming.Navigator;

public class GetUserFlatCatsEvent : IPacketEvent
{
    private readonly INavigatorService _navigatorService;

    public GetUserFlatCatsEvent(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _navigatorService.GetUserFlatCategories(session);
}
