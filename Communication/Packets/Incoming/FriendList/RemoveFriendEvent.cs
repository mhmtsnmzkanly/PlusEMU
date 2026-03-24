using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class RemoveFriendEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var messenger = habbo?.Messenger;
        if (messenger == null)
            return Task.CompletedTask;

        var amount = packet.ReadInt();
        if (amount > 100)
            amount = 100;
        else if (amount < 0)
            return Task.CompletedTask;
        for (var i = 0; i < amount; i++)
        {
            var id = packet.ReadInt();
            var friend = messenger.GetFriend(id);
            if (friend == null) continue;
            messenger.RemoveFriend(friend);
        }
        return Task.CompletedTask;
    }
}
