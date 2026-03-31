using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms;

public interface IRoomService
{
    Task PrepareRoom(GameClient session, uint roomId, string password);
    Task<RoomData?> CreateRoom(GameClient session, string name, string description, string modelName, int category, int maxVisitors, int tradeSettings);
    Task EnterRoom(GameClient session);
    Task<bool> FinalizeRoomEntry(GameClient session);
    Task LeaveRoom(GameClient session, bool notifyUser = true);
    Task KickFromRoom(GameClient session, bool notifyUser = true);
    Task HandleDisconnect(GameClient session);
}
