using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.PathFinding;

namespace Plus.Communication.Packets.Incoming.Rooms.Avatar;

internal class LookToEvent : RoomPacketEvent
{
    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var currentRoom))
            return Task.CompletedTask;

        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var user) || user == null)
            return Task.CompletedTask;
        if (user.IsAsleep)
            return Task.CompletedTask;
        user.UnIdle();
        var x = packet.ReadInt();
        var y = packet.ReadInt();
        if (x == user.X && y == user.Y || user.IsWalking || user.RidingHorse)
            return Task.CompletedTask;
        var rot = Rotation.Calculate(user.X, user.Y, x, y);
        user.SetRot(rot, false);
        user.UpdateNeeded = true;
        if (user.RidingHorse)
        {
            if (currentRoom.GetRoomUserManager().TryGetRoomUserByVirtualId(user.HorseId, out var horse) && horse != null)
            {
                horse.SetRot(rot, false);
                horse.UpdateNeeded = true;
            }
        }
        return Task.CompletedTask;
    }
}
