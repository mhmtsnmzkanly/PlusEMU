using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuideSessionEndedComposer : IServerPacket
{
    public const int SomethingWrong = 0;
    public const int HelpCaseClosed = 1;

    private readonly int _reason;

    public GuideSessionEndedComposer(int reason) => _reason = reason;

    public uint MessageId => ServerPacketHeader.GuideSessionEndedComposer;

    public void Compose(IOutgoingPacket packet) => packet.WriteInteger(_reason);
}
