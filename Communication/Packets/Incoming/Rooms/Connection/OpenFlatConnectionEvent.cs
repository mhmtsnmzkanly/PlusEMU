using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Connection;

public class OpenFlatConnectionEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var roomId = packet.ReadUInt();
        var password = packet.ReadString();
        habbo.PrepareRoom(roomId, password);
        return Task.CompletedTask;
    }
}
