using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Friends;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class DeclineFriendEvent : IPacketEvent
{
    private readonly IMessengerService _messengerService;

    public DeclineFriendEvent(IMessengerService messengerService)
    {
        _messengerService = messengerService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var declineAll = packet.ReadBool();
        var amount = packet.ReadInt();
        var requestIds = new List<int>(Math.Max(amount, 0));
        for (var i = 0; i < amount; i++)
            requestIds.Add(packet.ReadInt());

        return _messengerService.DeclineFriendRequests(session, declineAll, requestIds);
    }
}
