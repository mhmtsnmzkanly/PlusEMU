using Microsoft.Extensions.Logging;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal class MoveAvatarEvent : IPacketEvent
{
    private readonly ILogger<MoveAvatarEvent> _logger;

    public MoveAvatarEvent(ILogger<MoveAvatarEvent> logger)
    {
        _logger = logger;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var moveX = packet.ReadInt();
        var moveY = packet.ReadInt();
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var room))
        {
            _logger.LogDebug("MoveAvatar ignored: no active room. SessionId={sessionId}, UserId={userId}, Target=({x},{y})", session.Id, session.GetHabboOrNull()?.Id, moveX, moveY);
            return Task.CompletedTask;
        }

        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var user) || user == null || !user.CanWalk)
        {
            _logger.LogDebug("MoveAvatar ignored: user unavailable or cannot walk. SessionId={sessionId}, UserId={userId}, RoomId={roomId}, Target=({x},{y}), UserFound={userFound}, CanWalk={canWalk}",
                session.Id, habbo.Id, room.RoomId, moveX, moveY, user != null, user?.CanWalk ?? false);
            return Task.CompletedTask;
        }
        if (moveX == user.X && moveY == user.Y)
            return Task.CompletedTask;
        if (user.RidingHorse)
        {
            if (room.GetRoomUserManager().TryGetRoomUserByVirtualId(user.HorseId, out var horse) && horse != null)
                horse.MoveTo(moveX, moveY);
        }
        user.MoveTo(moveX, moveY);
        return Task.CompletedTask;
    }
}
