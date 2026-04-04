using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Camera;

public sealed class CameraPublishWaitMessageComposer : IServerPacket
{
    private readonly bool _isOk;
    private readonly int _cooldownSeconds;
    private readonly string _extraDataId;

    public CameraPublishWaitMessageComposer(bool isOk, int cooldownSeconds, string extraDataId)
    {
        _isOk = isOk;
        _cooldownSeconds = cooldownSeconds;
        _extraDataId = extraDataId;
    }

    public uint MessageId => ServerPacketHeader.CameraPublishWaitMessageComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteBoolean(_isOk);
        packet.WriteInteger(_cooldownSeconds);
        if (!string.IsNullOrEmpty(_extraDataId))
            packet.WriteString(_extraDataId);
    }
}
