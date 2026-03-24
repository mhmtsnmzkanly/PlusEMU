using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class RequestFriendEvent : IPacketEvent
{
    private readonly IQuestManager _questManager;
    private readonly IMessengerDataLoader _messengerDataLoader;

    public RequestFriendEvent(IQuestManager questManager, IMessengerDataLoader messengerDataLoader)
    {
        _questManager = questManager;
        _messengerDataLoader = messengerDataLoader;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var messenger = session.GetHabbo()?.Messenger;
        if (messenger == null)
            return;

        var (userId, blocked) = await _messengerDataLoader.CanReceiveFriendRequests(packet.ReadString());
        if (userId == 0 || blocked)
            return;

        messenger.SendFriendRequest(userId);
        _questManager.ProgressUserQuest(session, QuestType.SocialFriend);
        return;
    }
}
