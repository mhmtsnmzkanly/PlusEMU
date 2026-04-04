using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuideSessionStartedComposer : IServerPacket
{
    private readonly Habbo _requester;
    private readonly Habbo _helper;

    public GuideSessionStartedComposer(Habbo requester, Habbo helper)
    {
        _requester = requester;
        _helper = helper;
    }

    public uint MessageId => ServerPacketHeader.GuideSessionStartedComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_requester.Id);
        packet.WriteString(_requester.Username);
        packet.WriteString(_requester.Look);
        packet.WriteInteger(_helper.Id);
        packet.WriteString(_helper.Username);
        packet.WriteString(_helper.Look);
    }
}
