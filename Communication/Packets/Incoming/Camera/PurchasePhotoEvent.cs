using Plus.HabboHotel.Camera;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Camera;

internal class PurchasePhotoEvent : IPacketEvent
{
    private readonly ICameraService _cameraService;

    public PurchasePhotoEvent(ICameraService cameraService) => _cameraService = cameraService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _cameraService.PurchasePhoto(session);
}
