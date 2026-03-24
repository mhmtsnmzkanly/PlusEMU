using Plus.Communication.Packets.Outgoing.FriendList;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Messenger;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.FriendList;

internal class HabboSearchEvent : IPacketEvent
{
    private readonly ISearchResultFactory _searchResultFactory;

    public HabboSearchEvent(ISearchResultFactory searchResultFactory)
    {
        _searchResultFactory = searchResultFactory;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var messenger = session.GetHabbo()?.Messenger;
        if (messenger == null)
            return Task.CompletedTask;

        var query = StringCharFilter.Escape(packet.ReadString().Replace("%", ""));
        if (query.Length < 1 || query.Length > 100)
            return Task.CompletedTask;
        var friends = new List<SearchResult>();
        var othersUsers = new List<SearchResult>();
        var results = _searchResultFactory.GetSearchResult(query);
        foreach (var result in results.ToList())
        {
            if (messenger.FriendshipExists(result.UserId))
                friends.Add(result);
            else
                othersUsers.Add(result);
        }
        session.Send(new HabboSearchResultComposer(friends, othersUsers));
        return Task.CompletedTask;
    }
}
