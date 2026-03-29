using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Microsoft.Extensions.Logging;

namespace Plus.Communication.Packets.Incoming.Rooms.Connection;

internal class GoToFlatEvent : IPacketEvent
{
    private readonly IRoomService _roomService;
    private readonly ILogger<GoToFlatEvent> _logger;

    public GoToFlatEvent(IRoomService roomService, ILogger<GoToFlatEvent> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        _logger.LogInformation("GoToFlatEvent received for session {sessionId}. Build: {build}.", session.Id, session.ClientBuild ?? "<unknown>");
        return _roomService.EnterRoom(session);
    }
}
