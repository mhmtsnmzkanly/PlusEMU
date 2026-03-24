using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class HabboSearchEvent : IPacketEvent
{
    private readonly IMessengerService _messengerService;

    public HabboSearchEvent(IMessengerService messengerService)
    {
        _messengerService = messengerService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _messengerService.Search(session, packet.ReadString());
}
