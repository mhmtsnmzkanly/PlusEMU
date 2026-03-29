using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Microsoft.Extensions.Logging;

namespace Plus.Communication.Packets.Incoming.Rooms.Connection;

public class OpenFlatConnectionEvent : IPacketEvent
{
    private readonly IRoomService _roomService;
    private readonly ILogger<OpenFlatConnectionEvent> _logger;

    public OpenFlatConnectionEvent(IRoomService roomService, ILogger<OpenFlatConnectionEvent> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() == null)
            return Task.CompletedTask;

        var roomId = packet.ReadUInt();
        var password = packet.ReadString();
        _logger.LogInformation("OpenFlatConnectionEvent for session {sessionId}. RoomId: {roomId}. PasswordLength: {passwordLength}.", session.Id, roomId, password.Length);
        return _roomService.PrepareRoom(session, roomId, password);
    }
}
