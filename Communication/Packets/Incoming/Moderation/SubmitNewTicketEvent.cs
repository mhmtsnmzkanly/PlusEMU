using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class SubmitNewTicketEvent : IPacketEvent
{
    private readonly IModerationTicketService _moderationTicketService;

    public SubmitNewTicketEvent(IModerationTicketService moderationTicketService)
    {
        _moderationTicketService = moderationTicketService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var message = packet.ReadString();
        var category = packet.ReadInt();
        var reportedUserId = packet.ReadInt();
        var type = packet.ReadInt();
        var messagecount = packet.ReadInt();
        var chats = new List<ModerationTicketChatEntry>(messagecount);
        for (var i = 0; i < messagecount; i++)
        {
            var entryId = packet.ReadInt();
            chats.Add(new ModerationTicketChatEntry(entryId, packet.ReadString()));
        }

        return _moderationTicketService.Submit(session, message, category, reportedUserId, type, chats);
    }
}
