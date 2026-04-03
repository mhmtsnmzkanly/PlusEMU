using Plus.HabboHotel.Groups;

namespace Plus.HabboHotel.Rooms;

public interface IRoomDependencyResolver
{
    IRoomManager GetRoomManager();
    IRoomService GetRoomService();
    IGroupManager GetGroupManager();
}
