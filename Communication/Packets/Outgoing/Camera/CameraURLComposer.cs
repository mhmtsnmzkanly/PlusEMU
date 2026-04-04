using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Camera;

public sealed class CameraURLComposer : IServerPacket
{
    private readonly string _url;

    public CameraURLComposer(string url) => _url = url;

    public uint MessageId => ServerPacketHeader.CameraURLComposer;

    public void Compose(IOutgoingPacket packet) => packet.WriteString(_url);
}
