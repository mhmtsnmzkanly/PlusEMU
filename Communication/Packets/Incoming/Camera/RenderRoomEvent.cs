using Plus.HabboHotel.Camera;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Camera;

internal class RenderRoomEvent : IPacketEvent
{
    private readonly ICameraService _cameraService;

    public RenderRoomEvent(ICameraService cameraService) => _cameraService = cameraService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _cameraService.RenderRoom(session, false);
}
