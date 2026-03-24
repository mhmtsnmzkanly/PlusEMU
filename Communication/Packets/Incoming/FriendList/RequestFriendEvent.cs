using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class RequestFriendEvent : IPacketEvent
{
    private readonly IMessengerService _messengerService;

    public RequestFriendEvent(IMessengerService messengerService)
    {
        _messengerService = messengerService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _messengerService.SendFriendRequest(session, packet.ReadString());
}
