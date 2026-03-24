using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Navigator;

public interface INavigatorQueryService
{
    ICollection<RoomData> GetSearchResults(SearchResultList result, string query, GameClient session, int limit);
}
