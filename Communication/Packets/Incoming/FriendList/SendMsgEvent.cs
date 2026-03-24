using Plus.Communication.Packets.Outgoing.FriendList;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Chat.Filter;
using Plus.HabboHotel.Users.Messenger;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class SendMsgEvent : IPacketEvent
{
    private readonly IWordFilterManager _wordFilterManager;

    public SendMsgEvent(IWordFilterManager wordFilterManager)
    {
        _wordFilterManager = wordFilterManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var messenger = habbo?.Messenger;
        if (habbo == null || messenger == null)
            return Task.CompletedTask;

        var userId = packet.ReadInt();
        var friend = messenger.GetFriend(userId);
        if (friend == null)
        {
            session.Send(new InstantMessageErrorComposer(MessengerMessageErrors.NotFriends, userId));
            return Task.CompletedTask;
        }
        var message = _wordFilterManager.CheckMessage(packet.ReadString());
        if (string.IsNullOrWhiteSpace(message))
            return Task.CompletedTask;
        if (habbo.TimeMuted > 0)
        {
            session.SendNotification("Oops, you're currently muted - you cannot send messages.");
            return Task.CompletedTask;
        }

        var error = messenger.SendMessage(friend, message);
        if (error == MessageError.Flooding)
            session.SendNotification("You cannot send a message, you have flooded the console.\n\nYou can send a message in 60 seconds.");

        return Task.CompletedTask;
    }
}
