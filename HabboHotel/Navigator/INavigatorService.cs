using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Navigator;

public interface INavigatorService
{
    Task Initialize(GameClient session);
    Task UpdateSettings(GameClient session, uint roomId);
    Task GetUserFlatCategories(GameClient session);
    Task GetEventCategories(GameClient session);
    Task CanCreateRoom(GameClient session);
    Task GetGuestRoom(GameClient session, uint roomId, bool enter, bool forward);
    Task Search(GameClient session, string category, string search);
    Task CreateFlat(GameClient session, string name, string description, string modelName, int category, int maxVisitors, int tradeSettings);
    Task AddFavouriteRoom(GameClient session, uint roomId);
    Task RemoveFavouriteRoom(GameClient session, uint roomId);
    Task EditRoomPromotion(GameClient session, uint roomId, string name, string description);
}
