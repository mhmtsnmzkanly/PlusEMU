using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuideSessionRequesterRoomComposer : IServerPacket
{
    private readonly Room? _room;

    public GuideSessionRequesterRoomComposer(Room? room) => _room = room;

    public uint MessageId => ServerPacketHeader.GuideSessionRequesterRoomComposer;

    public void Compose(IOutgoingPacket packet) => packet.WriteInteger((int?)_room?.Id ?? 0);
}
