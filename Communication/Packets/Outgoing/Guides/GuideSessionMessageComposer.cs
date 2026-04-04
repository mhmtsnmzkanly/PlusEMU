using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuideSessionMessageComposer : IServerPacket
{
    private readonly GuideChatMessage _message;

    public GuideSessionMessageComposer(GuideChatMessage message) => _message = message;

    public uint MessageId => ServerPacketHeader.GuideSessionMessageComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteString(_message.Message);
        packet.WriteInteger(_message.UserId);
    }
}
