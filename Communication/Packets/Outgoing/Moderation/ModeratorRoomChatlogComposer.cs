using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat.Logs;
using Plus.Utilities;

namespace Plus.Communication.Packets.Outgoing.Moderation;

public class ModeratorRoomChatlogComposer : IServerPacket
{
    private readonly Room _room;
    private readonly ICollection<ChatlogEntry> _chats;
    public uint MessageId => ServerPacketHeader.ModeratorRoomChatlogComposer;

    public ModeratorRoomChatlogComposer(Room room, ICollection<ChatlogEntry> chats)
    {
        _room = room;
        _chats = chats;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteByte(1);
        packet.WriteShort(2); //Count
        packet.WriteString("roomName");
        packet.WriteByte(2);
        packet.WriteString(_room.Name);
        packet.WriteString("roomId");
        packet.WriteByte(1);
        packet.WriteUInteger(_room.Id);
        packet.WriteShort((short)_chats.Count);
        foreach (var entry in _chats)
        {
            var username = "Unknown";
            var player = entry.PlayerNullable();
            if (player != null)
                username = player.Username ?? "Unknown";
            packet.WriteString(UnixTimestamp.FromUnixTimestamp(entry.Timestamp).ToShortTimeString()); // time?
            packet.WriteInteger(entry.PlayerId); // User Id
            packet.WriteString(username); // Username
            packet.WriteString(!string.IsNullOrEmpty(entry.Message) ? entry.Message : "** user sent a blank message **"); // Message
            packet.WriteBoolean(false); //TODO, AI's?
        }
    }
}
