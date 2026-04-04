using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuideSessionPartnerIsPlayingComposer : IServerPacket
{
    private readonly bool _isPlaying;

    public GuideSessionPartnerIsPlayingComposer(bool isPlaying) => _isPlaying = isPlaying;

    public uint MessageId => ServerPacketHeader.GuideSessionPartnerIsPlayingComposer;

    public void Compose(IOutgoingPacket packet) => packet.WriteBoolean(_isPlaying);
}
