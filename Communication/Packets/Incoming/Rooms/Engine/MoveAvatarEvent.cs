using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal class MoveAvatarEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var room))
            return Task.CompletedTask;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null || !user.CanWalk)
            return Task.CompletedTask;
        var moveX = packet.ReadInt();
        var moveY = packet.ReadInt();
        if (moveX == user.X && moveY == user.Y)
            return Task.CompletedTask;
        if (user.RidingHorse)
        {
            var horse = room.GetRoomUserManager().GetRoomUserByVirtualId(user.HorseId);
            if (horse != null)
                horse.MoveTo(moveX, moveY);
        }
        user.MoveTo(moveX, moveY);
        return Task.CompletedTask;
    }
}
