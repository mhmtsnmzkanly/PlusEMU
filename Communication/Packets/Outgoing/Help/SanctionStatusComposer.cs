using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Outgoing.Help;

public class SanctionStatusComposer : IServerPacket
{
    private readonly SanctionStatusData _data;

    public uint MessageId => ServerPacketHeader.SanctionStatusComposer;

    public SanctionStatusComposer(SanctionStatusData data)
    {
        _data = data;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteBoolean(_data.HasCurrentSanction);
        packet.WriteBoolean(_data.UsesCustomMessage);
        packet.WriteString(_data.CurrentSanctionText);
        packet.WriteInteger(_data.CurrentSanctionHours);
        packet.WriteInteger(_data.ProbationDaysLeft);
        packet.WriteString(_data.NextSanctionText);
        packet.WriteString(_data.InfoTitle);
        packet.WriteInteger(_data.CautionCount);
        packet.WriteString(_data.Disclaimer);
        packet.WriteInteger(_data.BanCount);
        packet.WriteInteger(_data.TradeLockCount);
        packet.WriteBoolean(_data.IsMuted);
    }
}
