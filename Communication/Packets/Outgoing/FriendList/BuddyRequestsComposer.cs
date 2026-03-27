using Plus.HabboHotel.Cache;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Messenger;

namespace Plus.Communication.Packets.Outgoing.FriendList;

public class BuddyRequestsComposer : IServerPacket
{
    private readonly ICollection<MessengerRequest> _requests;
    private readonly ICacheManager _cacheManager;
    public uint MessageId => ServerPacketHeader.BuddyRequestsComposer;

    public BuddyRequestsComposer(ICollection<MessengerRequest> requests, ICacheManager cacheManager)
    {
        _requests = requests;
        _cacheManager = cacheManager;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_requests.Count);
        packet.WriteInteger(_requests.Count);
        foreach (var request in _requests)
        {
            packet.WriteInteger(request.FromId);
            packet.WriteString(request.Username);
            var user = _cacheManager.GenerateUser(request.FromId);
            packet.WriteString(user != null ? user.Look : "");
        }
    }
}