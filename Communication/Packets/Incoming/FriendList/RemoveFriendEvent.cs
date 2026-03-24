using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Friends;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class RemoveFriendEvent : IPacketEvent
{
    private readonly IMessengerService _messengerService;

    public RemoveFriendEvent(IMessengerService messengerService)
    {
        _messengerService = messengerService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var amount = packet.ReadInt();
        if (amount > 100)
            amount = 100;
        if (amount < 0)
            return Task.CompletedTask;

        var friendIds = new List<int>(amount);
        for (var i = 0; i < amount; i++)
            friendIds.Add(packet.ReadInt());

        return _messengerService.RemoveFriends(session, friendIds);
    }
}
