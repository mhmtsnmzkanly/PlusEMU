using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.Utilities;

namespace Plus.Communication.Packets.Outgoing.Moderation;

public class ModeratorSupportTicketComposer : IServerPacket
{
    private readonly int _id;
    private readonly ModerationTicket _ticket;
    public uint MessageId => ServerPacketHeader.ModeratorSupportTicketComposer;

    public ModeratorSupportTicketComposer(int id, ModerationTicket ticket)
    {
        _id = id;
        _ticket = ticket;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_ticket.Id); // Id
        packet.WriteInteger(_ticket.GetStatus(_id)); // Tab ID
        packet.WriteInteger(_ticket.Type); // Type
        packet.WriteInteger(_ticket.Category); // Category
        packet.WriteInteger(Convert.ToInt32((DateTime.Now - UnixTimestamp.FromUnixTimestamp(_ticket.Timestamp)).TotalMilliseconds)); // This should fix the overflow?
        packet.WriteInteger(_ticket.Priority); // Priority
        packet.WriteInteger(0); //??
        packet.WriteInteger(_ticket.Sender?.Id ?? 0); // Sender ID
        //base.WriteInteger(1);
        packet.WriteString(_ticket.Sender == null ? string.Empty : _ticket.Sender.Username); // Sender Name
        packet.WriteInteger(_ticket.Reported?.Id ?? _ticket.ReportedUserId); // Reported ID
        packet.WriteString(_ticket.Reported?.Username ?? _ticket.ReportedUsername); // Reported Name
        packet.WriteInteger(_ticket.Moderator?.Id ?? 0); // Moderator ID
        packet.WriteString(_ticket.Moderator == null ? string.Empty : _ticket.Moderator.Username); // Mod Name
        packet.WriteString(_ticket.Issue); // Issue
        packet.WriteUInteger(_ticket.Room?.Id ?? 0); // Room Id
        packet.WriteInteger(0);
        {
            // push String
            // push Integer
            // push Integer
        }
    }
}
