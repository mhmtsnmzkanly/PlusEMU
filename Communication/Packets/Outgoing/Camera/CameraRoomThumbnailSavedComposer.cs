using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Camera;

public sealed class CameraRoomThumbnailSavedComposer : IServerPacket
{
    public uint MessageId => ServerPacketHeader.CameraRoomThumbnailSavedComposer;

    public void Compose(IOutgoingPacket packet)
    {
    }
}
