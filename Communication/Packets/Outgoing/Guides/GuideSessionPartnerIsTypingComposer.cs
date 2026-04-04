using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuideSessionPartnerIsTypingComposer : IServerPacket
{
    private readonly bool _typing;

    public GuideSessionPartnerIsTypingComposer(bool typing) => _typing = typing;

    public uint MessageId => ServerPacketHeader.GuideSessionPartnerIsTypingComposer;

    public void Compose(IOutgoingPacket packet) => packet.WriteBoolean(_typing);
}
