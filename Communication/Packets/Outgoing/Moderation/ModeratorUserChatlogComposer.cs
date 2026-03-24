using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat.Logs;
using Plus.HabboHotel.Users;
using Plus.Utilities;

namespace Plus.Communication.Packets.Outgoing.Moderation;

public class ModeratorUserChatlogComposer : IServerPacket
{
    private readonly Habbo _habbo;
    private readonly List<KeyValuePair<RoomData, List<ChatlogEntry>>> _chatlogs;
    public uint MessageId => ServerPacketHeader.ModeratorUserChatlogComposer;

    public ModeratorUserChatlogComposer(Habbo habbo, List<KeyValuePair<RoomData, List<ChatlogEntry>>> chatlogs)
    {
        _habbo = habbo;
        _chatlogs = chatlogs;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_habbo.Id);
        packet.WriteString(_habbo.Username);
        packet.WriteInteger(_chatlogs.Count); // Room Visits Count
        foreach (var chatlog in _chatlogs)
        {
            packet.WriteByte(1);
            packet.WriteShort(2); //Count
            packet.WriteString("roomName");
            packet.WriteByte(2);
            packet.WriteString(chatlog.Key.Name); // room name
            packet.WriteString("roomId");
            packet.WriteByte(1);
            packet.WriteUInteger(chatlog.Key.Id);
            packet.WriteShort((short)chatlog.Value.Count); // Chatlogs Count
            foreach (var entry in chatlog.Value)
            {
                var username = "NOT FOUND";
                var player = entry.PlayerNullable();
                if (player != null)
                    username = player.Username ?? "NOT FOUND";
                packet.WriteString(UnixTimestamp.FromUnixTimestamp(entry.Timestamp).ToShortTimeString());
                packet.WriteInteger(entry.PlayerId); // UserId of message
                packet.WriteString(username); // Username of message
                packet.WriteString(!string.IsNullOrEmpty(entry.Message) ? entry.Message : "** user sent a blank message **"); // Message
                packet.WriteBoolean(_habbo.Id == entry.PlayerId);
            }
        }
    }
}
