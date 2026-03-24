using Plus.Communication.Packets.Outgoing.FriendList;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class FollowFriendEvent : IPacketEvent
{
    private readonly IMessengerService _messengerService;

    public FollowFriendEvent(IMessengerService messengerService)
    {
        _messengerService = messengerService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _messengerService.FollowFriend(session, packet.ReadInt());
}
