using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuardianNewReportReceivedComposer : IServerPacket
{
    private readonly int _acceptTimerSeconds;

    public GuardianNewReportReceivedComposer(int acceptTimerSeconds) => _acceptTimerSeconds = acceptTimerSeconds;

    public uint MessageId => ServerPacketHeader.GuardianNewReportReceivedComposer;

    public void Compose(IOutgoingPacket packet) => packet.WriteInteger(_acceptTimerSeconds);
}
