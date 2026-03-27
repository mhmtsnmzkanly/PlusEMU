using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Rooms;

public interface IRoomFactory
{
    List<RoomData> GetRoomsDataByOwnerSortByName(int ownerId);
    bool TryGetData(uint roomId, out RoomData? data);
}
