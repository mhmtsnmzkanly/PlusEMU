using Plus.HabboHotel.Camera;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Camera;

internal class RenderRoomThumbnailEvent : IPacketEvent
{
    private readonly ICameraService _cameraService;

    public RenderRoomThumbnailEvent(ICameraService cameraService) => _cameraService = cameraService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _cameraService.RenderRoom(session, true);
}
