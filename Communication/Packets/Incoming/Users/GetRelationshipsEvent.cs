using Plus.Communication.Packets.Outgoing.Users;
using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Messenger;

namespace Plus.Communication.Packets.Incoming.Users;

internal class GetRelationshipsEvent : IPacketEvent
{
    private readonly MessengerDataLoader _messengerDataLoader;
    private readonly GameClientManager _gameClientManager;

    public GetRelationshipsEvent(MessengerDataLoader messengerDataLoader, GameClientManager gameClientManager)
    {
        _messengerDataLoader = messengerDataLoader;
        _gameClientManager = gameClientManager;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var messenger = session.GetHabbo()?.Messenger;
        var userId = packet.ReadInt();
        if (messenger == null)
        {
            session.Send(new GetRelationshipsComposer(userId, new Dictionary<int, (MessengerBuddy buddy, int count)>()));
            return;
        }

        var relationships = await messenger.GetRelationshipsForUserAsync(userId, _gameClientManager, _messengerDataLoader);
        session.Send(new GetRelationshipsComposer(userId, relationships));
    }
}
