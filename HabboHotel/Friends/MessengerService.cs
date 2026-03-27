using Plus.Communication.Packets.Outgoing.FriendList;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.Communication.Packets.Outgoing.Users;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat.Filter;
using Plus.HabboHotel.Users.Messenger;
using Plus.Utilities;

namespace Plus.HabboHotel.Friends;

internal class MessengerService : IMessengerService
{
    private readonly IMessengerDataLoader _messengerDataLoader;
    private readonly IQuestService _questService;
    private readonly IWordFilterManager _wordFilterManager;
    private readonly IGameClientManager _gameClientManager;
    private readonly ISearchResultFactory _searchResultFactory;
    private readonly IRoomManager _roomManager;

    public MessengerService(
        IMessengerDataLoader messengerDataLoader,
        IQuestService questService,
        IWordFilterManager wordFilterManager,
        IGameClientManager gameClientManager,
        ISearchResultFactory searchResultFactory,
        IRoomManager roomManager)
    {
        _messengerDataLoader = messengerDataLoader;
        _questService = questService;
        _wordFilterManager = wordFilterManager;
        _gameClientManager = gameClientManager;
        _searchResultFactory = searchResultFactory;
        _roomManager = roomManager;
    }

    public async Task Initialize(GameClient session)
    {
        var habbo = session.GetHabbo();
        var messenger = habbo?.Messenger;
        if (habbo == null || messenger == null)
            return;

        var friends = messenger.Friends.Values.ToList();
        session.Send(new MessengerInitComposer());

        if (!friends.Any())
        {
            session.Send(new BuddyListComposer(friends, 1, 0));
        }
        else
        {
            var page = 0;
            var pages = (friends.Count - 1) / 500 + 1;
            foreach (var batch in friends.Chunk(500))
            {
                session.Send(new BuddyListComposer(batch.ToList(), pages, page));
                page++;
            }
        }

        var messages = await _messengerDataLoader.GetAndDeleteOfflineMessages(habbo.Id);
        foreach (var (userId, report) in messages)
        foreach (var (message, secondsAgo) in report)
            session.Send(new NewConsoleMessageComposer(userId, message, secondsAgo));
    }

    public async Task SendFriendRequest(GameClient session, string username)
    {
        var habbo = session.GetHabbo();
        var messenger = habbo?.Messenger;
        if (messenger == null)
            return;

        var (userId, blocked) = await _messengerDataLoader.CanReceiveFriendRequests(username);
        if (userId == 0 || blocked)
            return;

        messenger.SendFriendRequest(userId);
        await _questService.ProgressUserQuest(session, QuestType.SocialFriend);
    }

    public Task AcceptFriendRequests(GameClient session, IReadOnlyCollection<int> requestIds)
    {
        var messenger = session.GetHabbo()?.Messenger;
        if (messenger == null)
            return Task.CompletedTask;

        foreach (var requestId in requestIds.Take(50))
            messenger.AcceptFriendRequest(requestId);

        return Task.CompletedTask;
    }

    public Task DeclineFriendRequests(GameClient session, bool declineAll, IReadOnlyCollection<int> requestIds)
    {
        var messenger = session.GetHabbo()?.Messenger;
        if (messenger == null)
            return Task.CompletedTask;

        if (!declineAll)
        {
            foreach (var requestId in requestIds.Take(1))
                messenger.DeclineFriendRequest(requestId);
            return Task.CompletedTask;
        }

        foreach (var request in messenger.Requests.Values.ToList())
            messenger.DeclineFriendRequest(request.FromId);

        return Task.CompletedTask;
    }

    public Task RemoveFriends(GameClient session, IReadOnlyCollection<int> friendIds)
    {
        var messenger = session.GetHabbo()?.Messenger;
        if (messenger == null)
            return Task.CompletedTask;

        foreach (var id in friendIds.Take(100))
        {
            var friend = messenger.GetFriend(id);
            if (friend != null)
                messenger.RemoveFriend(friend);
        }

        return Task.CompletedTask;
    }

    public Task SendMessage(GameClient session, int userId, string message)
    {
        var habbo = session.GetHabbo();
        var messenger = habbo?.Messenger;
        if (habbo == null || messenger == null)
            return Task.CompletedTask;

        var friend = messenger.GetFriend(userId);
        if (friend == null)
        {
            session.Send(new InstantMessageErrorComposer(MessengerMessageErrors.NotFriends, userId));
            return Task.CompletedTask;
        }

        var filteredMessage = _wordFilterManager.CheckMessage(message);
        if (string.IsNullOrWhiteSpace(filteredMessage))
            return Task.CompletedTask;

        if (habbo.TimeMuted > 0)
        {
            session.SendNotification("Oops, you're currently muted - you cannot send messages.");
            return Task.CompletedTask;
        }

        var error = messenger.SendMessage(friend, filteredMessage);
        if (error == MessageError.Flooding)
            session.SendNotification("You cannot send a message, you have flooded the console.\n\nYou can send a message in 60 seconds.");

        return Task.CompletedTask;
    }

    public async Task SendRoomInvite(GameClient session, IReadOnlyCollection<int> targetIds, string message)
    {
        var habbo = session.GetHabbo();
        var messenger = habbo?.Messenger;
        if (habbo == null || messenger == null)
            return;

        if (habbo.TimeMuted > 0)
        {
            session.SendNotification("Oops, you're currently muted - you cannot send room invitations.");
            return;
        }

        var escapedMessage = StringCharFilter.Escape(message);
        if (escapedMessage.Length > 121)
            escapedMessage = escapedMessage[..121];

        foreach (var userId in targetIds.Take(100))
        {
            if (!messenger.FriendshipExists(userId))
                continue;

            var client = _gameClientManager.GetClientByUserId(userId);
            var targetHabbo = client?.GetHabbo();
            if (client == null || targetHabbo == null || targetHabbo.AllowMessengerInvites || targetHabbo.AllowConsoleMessages == false)
                continue;

            client.Send(new RoomInviteComposer(habbo.Id, escapedMessage));
        }

        await _messengerDataLoader.LogRoomInvitation(habbo.Id, escapedMessage);
    }

    public Task Search(GameClient session, string query)
    {
        var messenger = session.GetHabbo()?.Messenger;
        if (messenger == null)
            return Task.CompletedTask;

        var escapedQuery = StringCharFilter.Escape(query.Replace("%", ""));
        if (escapedQuery.Length < 1 || escapedQuery.Length > 100)
            return Task.CompletedTask;

        var friends = new List<SearchResult>();
        var otherUsers = new List<SearchResult>();
        var results = _searchResultFactory.GetSearchResult(escapedQuery);
        foreach (var result in results)
        {
            if (messenger.FriendshipExists(result.UserId))
                friends.Add(result);
            else
                otherUsers.Add(result);
        }

        session.Send(new HabboSearchResultComposer(friends, otherUsers));
        return Task.CompletedTask;
    }

    public Task GetFriendRequests(GameClient session)
    {
        var messenger = session.GetHabbo()?.Messenger;
        if (messenger == null)
            return Task.CompletedTask;

        session.Send(new BuddyRequestsComposer(messenger.Requests.Values.ToList()));
        return Task.CompletedTask;
    }

    public async Task GetRelationships(GameClient session, int userId)
    {
        var messenger = session.GetHabbo()?.Messenger;
        if (messenger == null)
        {
            session.Send(new GetRelationshipsComposer(userId, new Dictionary<int, (MessengerBuddy buddy, int count)>()));
            return;
        }

        var client = _gameClientManager.GetClientByUserId(userId);
        Dictionary<int, (MessengerBuddy buddy, int count)> relationships;
        if (client?.GetHabbo()?.Messenger is { } targetMessenger)
        {
            relationships = HabboMessenger.GetRelationships(new(targetMessenger.Friends));
        }
        else
        {
            relationships = await _messengerDataLoader.GetRelationshipsForUserAsync(userId);
        }

        session.Send(new GetRelationshipsComposer(userId, relationships));
    }

    public async Task SetRelationship(GameClient session, int friendId, int relationshipType)
    {
        var habbo = session.GetHabbo();
        var messenger = habbo?.Messenger;
        if (habbo == null || messenger == null)
            return;

        var friend = messenger.GetFriend(friendId);
        if (friend == null)
        {
            session.Send(new BroadcastMessageAlertComposer("Oops, you can only set a relationship where a friendship exists."));
            return;
        }

        if (relationshipType is < 0 or > 3)
        {
            session.Send(new BroadcastMessageAlertComposer("Oops, you've chosen an invalid relationship type."));
            return;
        }

        friend.Relationship = relationshipType;
        await _messengerDataLoader.SetRelationship(habbo.Id, friend.Id, relationshipType);
        messenger.UpdateFriend(friend);
    }

    public Task FollowFriend(GameClient session, int buddyId)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || buddyId == 0 || buddyId == habbo.Id)
            return Task.CompletedTask;

        var client = _gameClientManager.GetClientByUserId(buddyId);
        var targetHabbo = client?.GetHabbo();
        if (targetHabbo == null)
            return Task.CompletedTask;

        if (!targetHabbo.InRoom)
        {
            session.Send(new FollowFriendFailedComposer(2));
            return Task.CompletedTask;
        }

        var targetRoom = targetHabbo.CurrentRoom;
        if (targetRoom == null || habbo.CurrentRoom?.RoomId == targetRoom.RoomId)
            return Task.CompletedTask;

        session.Send(new RoomForwardComposer(targetRoom.RoomId));
        return Task.CompletedTask;
    }

    public Task FindNewFriends(GameClient session)
    {
        var instance = _roomManager.TryGetRandomLoadedRoom();
        if (instance != null)
        {
            session.Send(new FindFriendsProcessResultComposer(true));
            session.Send(new RoomForwardComposer(instance.Id));
            return Task.CompletedTask;
        }

        session.Send(new FindFriendsProcessResultComposer(false));
        return Task.CompletedTask;
    }
}
