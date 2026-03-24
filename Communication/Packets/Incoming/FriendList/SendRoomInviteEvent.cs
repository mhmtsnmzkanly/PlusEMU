using Plus.Communication.Packets.Outgoing.FriendList;
using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class SendRoomInviteEvent : IPacketEvent
{
    private readonly IMessengerService _messengerService;

    public SendRoomInviteEvent(IMessengerService messengerService)
    {
        _messengerService = messengerService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var amount = packet.ReadInt();
        if (amount > 500)
            return; // don't send at all

        var targets = new List<int>();
        for (var i = 0; i < amount; i++)
        {
            var uid = packet.ReadInt();
            if (i < 100)
                targets.Add(uid);
        }

        await _messengerService.SendRoomInvite(session, targets, packet.ReadString());
    }
}
