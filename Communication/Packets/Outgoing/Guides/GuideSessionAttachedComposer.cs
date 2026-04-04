using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuideSessionAttachedComposer : IServerPacket
{
    private readonly bool _isHelper;
    private readonly string _message;
    private readonly int _waitTime;

    public GuideSessionAttachedComposer(bool isHelper, string message, int waitTime)
    {
        _isHelper = isHelper;
        _message = message;
        _waitTime = waitTime;
    }

    public uint MessageId => ServerPacketHeader.GuideSessionAttachedComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteBoolean(_isHelper);
        packet.WriteInteger(1);
        packet.WriteString(_message);
        packet.WriteInteger(_waitTime);
    }
}
