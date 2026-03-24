using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Friends;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class AcceptFriendEvent : IPacketEvent
{
    private readonly IMessengerService _messengerService;

    public AcceptFriendEvent(IMessengerService messengerService)
    {
        _messengerService = messengerService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var amount = packet.ReadInt();
        if (amount > 50)
            amount = 50;
        if (amount < 0)
            return Task.CompletedTask;

        var requestIds = new List<int>(amount);
        for (var i = 0; i < amount; i++)
            requestIds.Add(packet.ReadInt());

        return _messengerService.AcceptFriendRequests(session, requestIds);
    }
}
