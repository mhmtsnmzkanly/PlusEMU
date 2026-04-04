using Plus.HabboHotel.Camera;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Camera;

internal class InitCameraEvent : IPacketEvent
{
    private readonly ICameraService _cameraService;

    public InitCameraEvent(ICameraService cameraService) => _cameraService = cameraService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _cameraService.SendConfiguration(session);
}
