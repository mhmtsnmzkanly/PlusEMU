using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class AcceptFriendEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var messenger = session.GetHabbo()?.Messenger;
        if (messenger == null)
            return Task.CompletedTask;

        var amount = packet.ReadInt();
        if (amount > 50)
            amount = 50;
        else if (amount < 0)
            return Task.CompletedTask;

        for (var i = 0; i < amount; i++)
        {
            var requestId = packet.ReadInt();
            messenger.AcceptFriendRequest(requestId);
        }
        return Task.CompletedTask;
    }
}
