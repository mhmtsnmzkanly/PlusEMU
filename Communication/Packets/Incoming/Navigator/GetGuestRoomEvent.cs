using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Navigator;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Navigator;

internal class GetGuestRoomEvent : IPacketEvent
{
    private readonly INavigatorService _navigatorService;

    public GetGuestRoomEvent(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
        => _navigatorService.GetGuestRoom(session, packet.ReadUInt(), packet.ReadInt() == 1, packet.ReadInt() == 1);
}
