using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Navigator;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Navigator;

internal class EditRoomPromotionEvent : IPacketEvent
{
    private readonly INavigatorService _navigatorService;

    public EditRoomPromotionEvent(INavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
        => _navigatorService.EditRoomPromotion(session, packet.ReadUInt(), packet.ReadString(), packet.ReadString());
}
