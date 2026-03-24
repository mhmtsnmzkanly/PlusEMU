using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class DeclineFriendEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var messenger = habbo?.Messenger;
        if (messenger == null)
            return Task.CompletedTask;

        var declineAll = packet.ReadBool();
        packet.ReadInt(); //amount
        if (!declineAll)
        {
            var requestId = packet.ReadInt();
            messenger.DeclineFriendRequest(requestId);
        }
        else
        {
            foreach (var request in messenger.Requests.Values)
                messenger.DeclineFriendRequest(request.FromId);
        }
        return Task.CompletedTask;
    }
}
