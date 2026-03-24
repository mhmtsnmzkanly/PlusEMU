using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class GetFriendRequestsEvent : IPacketEvent
{
    private readonly IMessengerService _messengerService;

    public GetFriendRequestsEvent(IMessengerService messengerService)
    {
        _messengerService = messengerService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _messengerService.GetFriendRequests(session);
}
