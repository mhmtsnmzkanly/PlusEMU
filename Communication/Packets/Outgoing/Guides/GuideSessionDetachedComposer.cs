using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuideSessionDetachedComposer : IServerPacket
{
    public uint MessageId => ServerPacketHeader.GuideSessionDetachedComposer;

    public void Compose(IOutgoingPacket packet)
    {
    }
}
