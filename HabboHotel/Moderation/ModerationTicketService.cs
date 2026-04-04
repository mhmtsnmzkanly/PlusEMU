using Dapper;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.Utilities;

namespace Plus.HabboHotel.Moderation;

internal class ModerationTicketService : IModerationTicketService
{
    private readonly IModerationManager _moderationManager;
    private readonly IGameClientManager _clientManager;
    private readonly IDatabase _database;

    public ModerationTicketService(
        IModerationManager moderationManager,
        IGameClientManager clientManager,
        IDatabase database)
    {
        _moderationManager = moderationManager;
        _clientManager = clientManager;
        _database = database;
    }

    public Task SendOpenState(GameClient session)
    {
        session.Send(new CfhTopicsInitComposer(_moderationManager.UserActionPresets));
        TrySendPendingTicketNotification(session);
        return Task.CompletedTask;
    }

    public async Task Submit(GameClient session, string message, int category, int reportedUserId, int type, IReadOnlyCollection<string> reportedChats)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        _moderationManager.TryGetTopicAction(category, out var topicAction);

        if (_moderationManager.UserHasTickets(habbo.Id)
            && _moderationManager.TryGetTicketBySenderId(habbo.Id, out var pendingTicket)
            && pendingTicket != null)
        {
            TrySendPendingTicketNotification(session);
            return;
        }

        if (IsAutoReplyTopic(topicAction))
        {
            session.SendNotification(GetSubmitAcknowledgement(category, topicAction));
            return;
        }

        var reportedUser = reportedUserId > 0
            ? _clientManager.GetClientByUserId(reportedUserId)?.GetHabbo()
            : null;
        string? reportedUsername = null;

        if (reportedUserId > 0 && reportedUser == null)
        {
            using var lookupConnection = _database.Connection();
            reportedUsername = await lookupConnection.QueryFirstOrDefaultAsync<string>(
                "SELECT `username` FROM `users` WHERE `id` = @userId LIMIT 1",
                new { userId = reportedUserId });
        }

        habbo.TryGetCurrentRoom(out var currentRoom);
        var issueText = BuildTicketIssue(message, category);

        var ticket = new ModerationTicket(
            1,
            type,
            category,
            UnixTimestamp.GetNow(),
            GetTicketPriority(topicAction),
            habbo,
            reportedUser,
            reportedUserId,
            reportedUsername,
            issueText,
            currentRoom,
            reportedChats.ToList());

        if (!_moderationManager.TryAddTicket(ticket))
            return;

        using var connection = _database.Connection();
        await connection.ExecuteAsync(
            "UPDATE `user_info` SET `cfhs` = `cfhs` + 1 WHERE `user_id` = @userId LIMIT 1",
            new { userId = habbo.Id });

        session.SendNotification(GetSubmitAcknowledgement(category, topicAction));
        _clientManager.ModAlert("A new support ticket has been submitted!");
        _clientManager.SendPacket(new ModeratorSupportTicketComposer(habbo.Id, ticket), "mod_tool");
    }

    public async Task Close(GameClient session, int result, int ticketId)
    {
        var moderator = session.GetHabbo();
        if (!(moderator?.Permissions?.HasRight("mod_tool") ?? false))
            return;

        if (!_moderationManager.TryGetTicket(ticketId, out var ticket) || ticket == null)
            return;
        if (ticket.Moderator?.Id != moderator.Id)
            return;

        var client = _clientManager.GetClientByUserId(ticket.Sender.Id);
        client?.Send(new ModeratorSupportTicketResponseComposer(result));
        client?.SendNotification(GetCloseAcknowledgement(result));

        if (result == 2)
        {
            using var connection = _database.Connection();
            await connection.ExecuteAsync(
                "UPDATE `user_info` SET `cfhs_abusive` = `cfhs_abusive` + 1 WHERE `user_id` = @senderId LIMIT 1",
                new { senderId = ticket.Sender.Id });
        }

        ticket.Answered = true;
        _clientManager.SendPacket(new ModeratorSupportTicketComposer(moderator.Id, ticket), "mod_tool");
    }

    public Task Pick(GameClient session, int ticketId)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null || !habbo.Permissions.HasRight("mod_tool"))
            return Task.CompletedTask;

        if (!_moderationManager.TryGetTicket(ticketId, out var ticket) || ticket == null)
            return Task.CompletedTask;

        ticket.Moderator = habbo;
        _clientManager.SendPacket(new ModeratorSupportTicketComposer(habbo.Id, ticket), "mod_tool");
        return Task.CompletedTask;
    }

    public Task Release(GameClient session, IReadOnlyCollection<int> ticketIds)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null || !habbo.Permissions.HasRight("mod_tool"))
            return Task.CompletedTask;

        foreach (var ticketId in ticketIds)
        {
            if (!_moderationManager.TryGetTicket(ticketId, out var ticket) || ticket == null)
                continue;

            ticket.Moderator = null;
            _clientManager.SendPacket(new ModeratorSupportTicketComposer(habbo.Id, ticket), "mod_tool");
        }

        return Task.CompletedTask;
    }

    public Task DeletePendingCalls(GameClient session)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        if (_moderationManager.UserHasTickets(habbo.Id)
            && _moderationManager.TryGetTicketBySenderId(habbo.Id, out var pendingTicket)
            && pendingTicket != null)
        {
            pendingTicket.Answered = true;
            session.SendNotification("Your pending help request has been closed.");
            _clientManager.SendPacket(new ModeratorSupportTicketComposer(habbo.Id, pendingTicket), "mod_tool");
        }

        return Task.CompletedTask;
    }

    private void TrySendPendingTicketNotification(GameClient session)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return;

        if (_moderationManager.UserHasTickets(habbo.Id)
            && _moderationManager.TryGetTicketBySenderId(habbo.Id, out var pendingTicket)
            && pendingTicket != null)
            session.SendNotification("You already have a pending help request.");
    }

    private string GetSubmitAcknowledgement(int category, ModerationPresetActions? action = null)
    {
        if (action == null)
            _moderationManager.TryGetTopicAction(category, out action);

        if (action != null && !string.IsNullOrWhiteSpace(action.MessageText))
            return action.MessageText;

        return "Your help request has been submitted.";
    }

    private static bool IsAutoReplyTopic(ModerationPresetActions? action) =>
        action != null && string.Equals(action.Type, "auto_reply", StringComparison.OrdinalIgnoreCase);

    private static int GetTicketPriority(ModerationPresetActions? action) =>
        action != null && string.Equals(action.Type, "mods_till_logout", StringComparison.OrdinalIgnoreCase)
            ? 2
            : 1;

    private static string GetCloseAcknowledgement(int result) =>
        result switch
        {
            1 => "Your help request was reviewed and closed as non-actionable.",
            2 => "Your help request was reviewed and marked abusive.",
            3 => "Your help request was reviewed and resolved.",
            _ => "Your help request was reviewed and closed."
        };

    private string BuildTicketIssue(string message, int category)
    {
        var trimmed = StringCharFilter.Escape((message ?? string.Empty).Trim());
        if (!_moderationManager.TryGetTopicCaption(category, out var caption) || string.IsNullOrWhiteSpace(caption))
            return trimmed;

        return string.IsNullOrWhiteSpace(trimmed)
            ? $"[{caption}]"
            : $"[{caption}] {trimmed}";
    }
}
