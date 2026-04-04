using Plus.HabboHotel.Camera;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Camera;

internal class PublishPhotoEvent : IPacketEvent
{
    private readonly ICameraService _cameraService;

    public PublishPhotoEvent(ICameraService cameraService) => _cameraService = cameraService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _cameraService.PublishPhoto(session);
}
