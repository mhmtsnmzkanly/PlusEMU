using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuideSessionInvitedToGuideRoomComposer : IServerPacket
{
    private readonly Room? _room;

    public GuideSessionInvitedToGuideRoomComposer(Room? room) => _room = room;

    public uint MessageId => ServerPacketHeader.GuideSessionInvitedToGuideRoomComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger((int?)_room?.Id ?? 0);
        packet.WriteString(_room?.Name ?? string.Empty);
    }
}
