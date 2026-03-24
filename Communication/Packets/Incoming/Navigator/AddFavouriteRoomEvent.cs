using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Navigator;

namespace Plus.Communication.Packets.Incoming.Navigator;

public class AddFavouriteRoomEvent : IPacketEvent
{
    private readonly INavigatorService _navigatorService;

    public AddFavouriteRoomEvent(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _navigatorService.AddFavouriteRoom(session, packet.ReadUInt());
}
