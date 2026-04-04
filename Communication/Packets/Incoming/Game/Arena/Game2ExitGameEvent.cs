using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Game.Arena;

internal class Game2ExitGameEvent : IPacketEvent
{
    private readonly IGuideService _guideService;

    public Game2ExitGameEvent(IGuideService guideService) => _guideService = guideService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _guideService.SetPlaying(session, false);
}
