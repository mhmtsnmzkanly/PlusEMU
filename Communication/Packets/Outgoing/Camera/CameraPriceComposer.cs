using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Camera;

public sealed class CameraPriceComposer : IServerPacket
{
    private readonly int _credits;
    private readonly int _points;
    private readonly int _pointsType;

    public CameraPriceComposer(int credits, int points, int pointsType)
    {
        _credits = credits;
        _points = points;
        _pointsType = pointsType;
    }

    public uint MessageId => ServerPacketHeader.CameraPriceComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_credits);
        packet.WriteInteger(_points);
        packet.WriteInteger(_pointsType);
    }
}
