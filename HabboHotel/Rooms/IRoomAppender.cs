using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Rooms;

public interface IRoomAppender
{
    void WriteRoom(IOutgoingPacket packet, RoomData data, RoomPromotion? promotion);
}
