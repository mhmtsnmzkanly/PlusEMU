using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class CallForHelpPendingCallsDeletedEvent : IPacketEvent
{
    private readonly IModerationManager _moderationManager;
    private readonly IGameClientManager _clientManager;

    public CallForHelpPendingCallsDeletedEvent(IModerationManager moderationManager, IGameClientManager clientManager)
    {
        _moderationManager = moderationManager;
        _clientManager = clientManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        if (_moderationManager.UserHasTickets(habbo.Id))
        {
            var pendingTicket = _moderationManager.GetTicketBySenderId(habbo.Id);
            if (pendingTicket != null)
            {
                pendingTicket.Answered = true;
                _clientManager.SendPacket(new ModeratorSupportTicketComposer(habbo.Id, pendingTicket), "mod_tool");
            }
        }
        return Task.CompletedTask;
    }
}
