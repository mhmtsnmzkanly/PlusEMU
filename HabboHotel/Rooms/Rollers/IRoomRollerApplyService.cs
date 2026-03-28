using System.Drawing;
using Plus.Communication.Packets;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public interface IRoomRollerApplyService
{
    IServerPacket CreateItemRollerUpdate(Room room, RoomItemHandling roomItemHandling, Item item, Point nextCoord, uint rollerId, double nextZ);
    IServerPacket CreateUserRollerUpdate(Room room, RoomItemHandling roomItemHandling, RoomUser user, Point nextCoord, uint rollerId, double nextZ);
}
