using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Rooms;
using Plus.Utilities;

namespace Plus.Communication.Packets.Outgoing.Moderation;

public class ModeratorTicketChatlogComposer : IServerPacket
{
    private readonly ModerationTicket _ticket;
    private readonly RoomData _roomData;
    private readonly double _timestamp;
    public uint MessageId => ServerPacketHeader.ModeratorTicketChatlogComposer;

    public ModeratorTicketChatlogComposer(ModerationTicket ticket, RoomData roomData, double timestamp)
    {
        _ticket = ticket;
        _roomData = roomData;
        _timestamp = timestamp;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_ticket.Id);
        packet.WriteInteger(_ticket.Sender?.Id ?? 0);
        packet.WriteInteger(_ticket.Reported?.Id ?? _ticket.ReportedUserId);
        packet.WriteUInteger(_roomData.Id);
        packet.WriteByte(1);
        packet.WriteShort(2); //Count
        packet.WriteString("roomName");
        packet.WriteByte(2);
        packet.WriteString(_roomData.Name);
        packet.WriteString("roomId");
        packet.WriteByte(1);
        packet.WriteUInteger(_roomData.Id);
        packet.WriteShort((short)_ticket.ReportedChats.Count);
        foreach (var chat in _ticket.ReportedChats)
        {
            packet.WriteString(UnixTimestamp.FromUnixTimestamp(_timestamp).ToShortTimeString());
            packet.WriteInteger(chat.EntryId > 0 ? chat.EntryId : _ticket.Id);
            packet.WriteString(_ticket.Reported?.Username ?? _ticket.ReportedUsername);
            packet.WriteString(chat.Message);
            packet.WriteBoolean(false);
        }
    }
}
