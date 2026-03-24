using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms;

public interface IRoomAccessService
{
    Task AssignRights(Room room, GameClient session, int userId);
    Task RemoveRights(Room room, GameClient session, IReadOnlyCollection<int> userIds);
    Task RemoveAllRights(Room room, GameClient session);
    Task RemoveMyRights(Room room, GameClient session);
    Task LetUserIn(Room room, GameClient session, string username, bool accepted);
    Task UnbanUser(GameClient session, int userId, int roomId);
    Task GetBannedUsers(GameClient session);
    Task ToggleMuteTool(GameClient session);
    Task GetRoomFilterList(GameClient session);
    Task ModifyRoomFilterList(GameClient session, bool added, string word);
    Task SaveEnforcedCategorySettings(GameClient session, uint roomId, int categoryId, int tradeSettings);
}
