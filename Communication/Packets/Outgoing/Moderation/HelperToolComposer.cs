using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Moderation;

public sealed class HelperToolComposer : IServerPacket
{
    private readonly bool _onDuty;
    private readonly int _helpersOnDuty;
    private readonly int _guardiansOnDuty;

    public HelperToolComposer(bool onDuty, int helpersOnDuty, int guardiansOnDuty)
    {
        _onDuty = onDuty;
        _helpersOnDuty = helpersOnDuty;
        _guardiansOnDuty = guardiansOnDuty;
    }

    public uint MessageId => ServerPacketHeader.HelperToolComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteBoolean(_onDuty);
        packet.WriteInteger(0);
        packet.WriteInteger(_helpersOnDuty);
        packet.WriteInteger(_guardiansOnDuty);
    }
}
