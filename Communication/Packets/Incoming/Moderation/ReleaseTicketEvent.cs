using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ReleaseTicketEvent : IPacketEvent
{
    public readonly IModerationManager _moderationManager;
    public readonly IGameClientManager _clientManager;

    public ReleaseTicketEvent(IModerationManager moderationManager, IGameClientManager clientManager)
    {
        _moderationManager = moderationManager;
        _clientManager = clientManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null || !habbo.Permissions.HasRight("mod_tool"))
            return Task.CompletedTask;
        var amount = packet.ReadInt();
        for (var i = 0; i < amount; i++)
        {
            if (!_moderationManager.TryGetTicket(packet.ReadInt(), out var ticket) || ticket == null)
                continue;
            ticket.Moderator = null;
            _clientManager.SendPacket(new ModeratorSupportTicketComposer(habbo.Id, ticket), "mod_tool");
        }
        return Task.CompletedTask;
    }
}
