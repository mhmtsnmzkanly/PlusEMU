using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuideSessionErrorComposer : IServerPacket
{
    public const int SomethingWrongRequest = 0;
    public const int NoHelpersAvailable = 1;
    public const int NoGuardiansAvailable = 2;

    private readonly int _errorCode;

    public GuideSessionErrorComposer(int errorCode) => _errorCode = errorCode;

    public uint MessageId => ServerPacketHeader.GuideSessionErrorComposer;

    public void Compose(IOutgoingPacket packet) => packet.WriteInteger(_errorCode);
}
