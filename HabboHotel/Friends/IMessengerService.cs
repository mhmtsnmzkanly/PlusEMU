using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Friends;

public interface IMessengerService
{
    Task Initialize(GameClient session);
    Task SendFriendRequest(GameClient session, string username);
    Task AcceptFriendRequests(GameClient session, IReadOnlyCollection<int> requestIds);
    Task DeclineFriendRequests(GameClient session, bool declineAll, IReadOnlyCollection<int> requestIds);
    Task RemoveFriends(GameClient session, IReadOnlyCollection<int> friendIds);
    Task SendMessage(GameClient session, int userId, string message);
    Task SendRoomInvite(GameClient session, IReadOnlyCollection<int> targetIds, string message);
    Task Search(GameClient session, string query);
    Task GetFriendRequests(GameClient session);
    Task GetRelationships(GameClient session, int userId);
    Task SetRelationship(GameClient session, int friendId, int relationshipType);
    Task FollowFriend(GameClient session, int buddyId);
    Task FindNewFriends(GameClient session);
}
