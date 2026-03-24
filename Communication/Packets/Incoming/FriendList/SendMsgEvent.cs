using Plus.Communication.Packets.Outgoing.FriendList;
using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class SendMsgEvent : IPacketEvent
{
    private readonly IMessengerService _messengerService;

    public SendMsgEvent(IMessengerService messengerService)
    {
        _messengerService = messengerService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var userId = packet.ReadInt();
        var message = packet.ReadString();
        return _messengerService.SendMessage(session, userId, message);
    }
}
