using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Camera;

public sealed class CameraPurchaseSuccesfullComposer : IServerPacket
{
    public uint MessageId => ServerPacketHeader.CameraPurchaseSuccesfullComposer;

    public void Compose(IOutgoingPacket packet)
    {
    }
}
