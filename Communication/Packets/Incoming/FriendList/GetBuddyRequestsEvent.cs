using Plus.Communication.Packets.Outgoing.FriendList;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Messenger;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class GetFriendRequestsEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var messenger = session.GetHabbo()?.Messenger;
        if (messenger == null)
            return Task.CompletedTask;

        ICollection<MessengerRequest> requests = messenger.Requests.Values.ToList();
        session.Send(new BuddyRequestsComposer(requests));
        return Task.CompletedTask;
    }
}
