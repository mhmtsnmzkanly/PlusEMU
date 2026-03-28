using System.Drawing;
using Plus.Communication.Packets;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms;

public class RoomRollerApplyService : IRoomRollerApplyService
{
    public IServerPacket CreateItemRollerUpdate(Room room, RoomItemHandling roomItemHandling, Item item, Point nextCoord, uint rollerId, double nextZ)
    {
        var message = new SlideObjectBundleComposer(item.GetX, item.GetY, item.GetZ, nextCoord.X, nextCoord.Y, nextZ, rollerId, 0, item.Id);
        roomItemHandling.SetFloorItem(item, nextCoord.X, nextCoord.Y, nextZ);
        return message;
    }

    public IServerPacket CreateUserRollerUpdate(Room room, RoomItemHandling roomItemHandling, RoomUser user, Point nextCoord, uint rollerId, double nextZ)
    {
        var message = new SlideObjectBundleComposer(user.X, user.Y, user.Z, nextCoord.X, nextCoord.Y, nextZ, rollerId, user.VirtualId, 0);
        room.GetGameMap().UpdateUserMovement(new(user.X, user.Y), new(nextCoord.X, nextCoord.Y), user);
        room.GetGameMap().GameMap[user.X, user.Y] = 1;
        user.X = nextCoord.X;
        user.Y = nextCoord.Y;
        user.Z = nextZ;
        room.GetGameMap().GameMap[user.X, user.Y] = 0;
        TriggerRollerUserWiredEvents(room, user.GetClient()?.GetHabbo(), nextCoord, rollerId);
        return message;
    }

    private static void TriggerRollerUserWiredEvents(Room room, Habbo? habbo, Point nextCoord, uint rollerId)
    {
        if (habbo == null)
            return;

        foreach (var item in room.GetGameMap().GetRoomItemForSquare(nextCoord.X, nextCoord.Y).ToList())
        {
            if (item == null)
                continue;

            room.GetWired().TriggerEvent(WiredBoxType.TriggerWalkOnFurni, habbo, item);
        }

        var roller = room.GetRoomItemHandler().GetItem(rollerId);
        if (roller != null)
            room.GetWired().TriggerEvent(WiredBoxType.TriggerWalkOffFurni, habbo, roller);
    }
}
