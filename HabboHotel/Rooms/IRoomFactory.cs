using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Rooms;

public interface IRoomFactory
{
    List<RoomData> GetRoomsDataByOwnerSortByName(int ownerId);
    bool TryGetData(uint roomId, out RoomData? data);
    RoomData CreateRoomData(GameClient session, string name, string description, int category, int maxVisitors, int tradeSettings, RoomModel model,
        string wallpaper = "0.0", string floor = "0.0", string landscape = "0.0", int wallthick = 0, int floorthick = 0);
}
