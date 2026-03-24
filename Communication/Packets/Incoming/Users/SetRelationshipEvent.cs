using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Database;
using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Users;

internal class SetRelationshipEvent : IPacketEvent
{
    private readonly IGameClientManager _clientManager;
    private readonly IDatabase _database;
    private readonly IMessengerDataLoader _messengerDataLoader;

    public SetRelationshipEvent(IGameClientManager clientManager, IDatabase database, IMessengerDataLoader messengerDataLoader)
    {
        _clientManager = clientManager;
        _database = database;
        _messengerDataLoader = messengerDataLoader;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var messenger = habbo?.Messenger;
        if (habbo == null || messenger == null)
            return;

        var user = packet.ReadInt();
        var type = packet.ReadInt();
        var friend = messenger.GetFriend(user);
        if (friend == null)
        {
            session.Send(new BroadcastMessageAlertComposer("Oops, you can only set a relationship where a friendship exists."));
            return;
        }
        if (type < 0 || type > 3)
        {
            session.Send(new BroadcastMessageAlertComposer("Oops, you've chosen an invalid relationship type."));
            return;
        }

        friend.Relationship = type;
        await _messengerDataLoader.SetRelationship(habbo.Id, friend.Id, friend.Relationship);
        messenger.UpdateFriend(friend);
        return;
    }
}
