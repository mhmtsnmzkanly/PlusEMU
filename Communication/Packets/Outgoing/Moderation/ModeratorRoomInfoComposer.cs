using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Outgoing.Moderation;

public class ModeratorRoomInfoComposer : IServerPacket
{
    private readonly RoomData _data;
    private readonly bool _ownerInRoom;
    public uint MessageId => ServerPacketHeader.ModeratorRoomInfoComposer;

    public ModeratorRoomInfoComposer(RoomData data, bool ownerInRoom)
    {
        _data = data;
        _ownerInRoom = ownerInRoom;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteUInteger(_data.Id);
        packet.WriteInteger(_data.UsersNow);
        packet.WriteBoolean(_ownerInRoom); // owner in room
        packet.WriteInteger(_data.OwnerId);
        packet.WriteString(_data.OwnerName ?? string.Empty);
        packet.WriteBoolean(true);
        packet.WriteString(_data.Name ?? string.Empty);
        packet.WriteString(_data.Description ?? string.Empty);
        packet.WriteInteger(_data.Tags.Count);
        foreach (var tag in _data.Tags) packet.WriteString(tag);
        packet.WriteBoolean(false);
    }
}
