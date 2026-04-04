using System.Collections.Concurrent;
using Plus.Communication.Packets.Outgoing.Guides;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.Utilities;

namespace Plus.HabboHotel.Guides;

internal sealed class GuideService : IGuideService
{
    private const string GuideToolPermission = "mod_tool";

    private readonly object _sync = new();
    private readonly IGameClientManager _clientManager;
    private readonly IModerationManager _moderationManager;
    private readonly IGuardianService _guardianService;
    private readonly ConcurrentDictionary<int, byte> _watchedUsers = new();
    private readonly Dictionary<int, bool> _helpers = new();
    private readonly Dictionary<int, GuideSession> _sessionsByUserId = new();
    private readonly Queue<int> _resolvedWaitingTimes = new();

    public GuideService(IGameClientManager clientManager, IModerationManager moderationManager, IGuardianService guardianService)
    {
        _clientManager = clientManager;
        _moderationManager = moderationManager;
        _guardianService = guardianService;
    }

    public Task SendToolState(GameClient session, bool onDutyOverride = false, bool useOverride = false)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        WatchDisconnect(habbo);

        bool onDuty;
        int helpersOnDuty;
        lock (_sync)
        {
            PruneLocked();
            onDuty = useOverride ? onDutyOverride : (_helpers.TryGetValue(habbo.Id, out var busy) && !busy);
            helpersOnDuty = _helpers.Count;
        }

        session.Send(new HelperToolComposer(onDuty, helpersOnDuty, _guardianService.GuardiansOnDuty));
        return Task.CompletedTask;
    }

    public Task ConfigureDuty(GameClient session, bool onDuty, bool helperRequests, bool bullyReports)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo?.Permissions == null)
            return Task.CompletedTask;

        if (!habbo.Permissions.HasRight(GuideToolPermission) && !habbo.IsAmbassador)
            return Task.CompletedTask;

        WatchDisconnect(habbo);

        lock (_sync)
        {
            PruneLocked();

            if (!onDuty || !helperRequests)
            {
                if (_sessionsByUserId.TryGetValue(habbo.Id, out var activeSession) && activeSession.HelperId == habbo.Id)
                    return Task.CompletedTask;

                _helpers.Remove(habbo.Id);
                session.Send(new HelperToolComposer(false, _helpers.Count, _guardianService.GuardiansOnDuty));
                return Task.CompletedTask;
            }

            _helpers[habbo.Id] = false;
            session.Send(new HelperToolComposer(true, _helpers.Count, _guardianService.GuardiansOnDuty));
        }

        return Task.CompletedTask;
    }

    public Task RequestAssistance(GameClient session, int requestType, string message)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        string trimmedMessage = StringCharFilter.Escape((message ?? string.Empty).Trim());
        if (string.IsNullOrWhiteSpace(trimmedMessage))
        {
            session.Send(new GuideSessionErrorComposer(GuideSessionErrorComposer.SomethingWrongRequest));
            return Task.CompletedTask;
        }

        WatchDisconnect(habbo);

        lock (_sync)
        {
            PruneLocked();

            if (_sessionsByUserId.ContainsKey(habbo.Id))
            {
                session.Send(new GuideSessionErrorComposer(GuideSessionErrorComposer.SomethingWrongRequest));
                return Task.CompletedTask;
            }

            var guideSession = new GuideSession(habbo.Id, requestType, trimmedMessage, (int)UnixTimestamp.GetNow());
            if (!TryAttachNextHelperLocked(guideSession))
            {
                session.Send(new GuideSessionErrorComposer(GuideSessionErrorComposer.NoHelpersAvailable));
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }

    public Task HandleRequest(GameClient session, bool accepted)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        lock (_sync)
        {
            PruneLocked();

            if (!_sessionsByUserId.TryGetValue(habbo.Id, out var guideSession) || guideSession.HelperId != habbo.Id)
                return Task.CompletedTask;

            if (!accepted)
            {
                guideSession.DeclinedHelperIds.Add(habbo.Id);
                _helpers[habbo.Id] = false;
                session.Send(new GuideSessionEndedComposer(GuideSessionEndedComposer.HelpCaseClosed));
                session.Send(new GuideSessionDetachedComposer());

                guideSession.HelperId = null;
                if (!TryAttachNextHelperLocked(guideSession))
                {
                    if (TryGetClientLocked(guideSession.RequesterId, out var requesterClient))
                        requesterClient.Send(new GuideSessionErrorComposer(GuideSessionErrorComposer.NoHelpersAvailable));
                    RemoveSessionLocked(guideSession, GuideSessionEndedComposer.HelpCaseClosed, detachRequester: true, detachHelper: false);
                }

                return Task.CompletedTask;
            }

            if (!TryGetClientLocked(guideSession.RequesterId, out var requester) || !TryGetClientLocked(habbo.Id, out var helper))
            {
                RemoveSessionLocked(guideSession, GuideSessionEndedComposer.HelpCaseClosed);
                return Task.CompletedTask;
            }

            guideSession.Started = true;
            _helpers[habbo.Id] = true;
            int waitTime = (int)Math.Max(UnixTimestamp.GetNow() - guideSession.CreatedAt, 0);
            _resolvedWaitingTimes.Enqueue(waitTime);
            while (_resolvedWaitingTimes.Count > 50)
                _resolvedWaitingTimes.Dequeue();

            var startPacket = new GuideSessionStartedComposer(requester.GetHabbo(), helper.GetHabbo());
            requester.Send(startPacket);
            helper.Send(startPacket);
        }

        return Task.CompletedTask;
    }

    public Task SendSessionMessage(GameClient session, string message)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        string trimmedMessage = StringCharFilter.Escape((message ?? string.Empty).Trim());
        if (string.IsNullOrWhiteSpace(trimmedMessage))
            return Task.CompletedTask;

        lock (_sync)
        {
            PruneLocked();

            if (!_sessionsByUserId.TryGetValue(habbo.Id, out var guideSession))
                return Task.CompletedTask;

            var chatMessage = new GuideChatMessage(habbo.Id, trimmedMessage, (int)UnixTimestamp.GetNow());
            guideSession.Messages.Add(chatMessage);
            var packet = new GuideSessionMessageComposer(chatMessage);

            if (TryGetClientLocked(guideSession.RequesterId, out var requester))
                requester.Send(packet);

            if (guideSession.HelperId is { } helperId && TryGetClientLocked(helperId, out var helper))
                helper.Send(packet);
        }

        return Task.CompletedTask;
    }

    public Task SetTyping(GameClient session, bool typing)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        lock (_sync)
        {
            PruneLocked();

            if (!_sessionsByUserId.TryGetValue(habbo.Id, out var guideSession))
                return Task.CompletedTask;

            int partnerUserId = guideSession.RequesterId == habbo.Id
                ? guideSession.HelperId ?? 0
                : guideSession.RequesterId;
            if (partnerUserId == 0 || !TryGetClientLocked(partnerUserId, out var partnerClient))
                return Task.CompletedTask;

            partnerClient.Send(new GuideSessionPartnerIsTypingComposer(typing));
        }

        return Task.CompletedTask;
    }

    public Task SetPlaying(GameClient session, bool isPlaying)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        lock (_sync)
        {
            PruneLocked();

            if (!_sessionsByUserId.TryGetValue(habbo.Id, out var guideSession))
                return Task.CompletedTask;

            int partnerUserId = guideSession.RequesterId == habbo.Id
                ? guideSession.HelperId ?? 0
                : guideSession.RequesterId;
            if (partnerUserId == 0 || !TryGetClientLocked(partnerUserId, out var partnerClient))
                return Task.CompletedTask;

            partnerClient.Send(new GuideSessionPartnerIsPlayingComposer(isPlaying));
        }

        return Task.CompletedTask;
    }

    public Task SendRequesterRoom(GameClient session)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        lock (_sync)
        {
            PruneLocked();

            if (!_sessionsByUserId.TryGetValue(habbo.Id, out var guideSession) || guideSession.HelperId != habbo.Id)
                return Task.CompletedTask;

            if (!TryGetClientLocked(guideSession.RequesterId, out var requesterClient))
                return Task.CompletedTask;

            requesterClient.GetHabbo().TryGetCurrentRoom(out var requesterRoom);
            session.Send(new GuideSessionRequesterRoomComposer(requesterRoom));
        }

        return Task.CompletedTask;
    }

    public Task InviteRequesterToRoom(GameClient session)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        lock (_sync)
        {
            PruneLocked();

            if (!_sessionsByUserId.TryGetValue(habbo.Id, out var guideSession) || guideSession.HelperId != habbo.Id)
                return Task.CompletedTask;

            if (!TryGetClientLocked(guideSession.RequesterId, out var requesterClient))
                return Task.CompletedTask;

            habbo.TryGetCurrentRoom(out var helperRoom);
            var packet = new GuideSessionInvitedToGuideRoomComposer(helperRoom);
            requesterClient.Send(packet);
            session.Send(packet);
        }

        return Task.CompletedTask;
    }

    public Task CancelRequest(GameClient session)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        lock (_sync)
        {
            PruneLocked();

            if (_sessionsByUserId.TryGetValue(habbo.Id, out var guideSession) && guideSession.RequesterId == habbo.Id)
                RemoveSessionLocked(guideSession, GuideSessionEndedComposer.HelpCaseClosed);
        }

        return Task.CompletedTask;
    }

    public Task CloseRequest(GameClient session)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        lock (_sync)
        {
            PruneLocked();

            if (_sessionsByUserId.TryGetValue(habbo.Id, out var guideSession))
                RemoveSessionLocked(guideSession, GuideSessionEndedComposer.HelpCaseClosed);
        }

        return Task.CompletedTask;
    }

    public Task ReportPartner(GameClient session, string message)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        string trimmedMessage = StringCharFilter.Escape((message ?? string.Empty).Trim());
        lock (_sync)
        {
            PruneLocked();

            if (!_sessionsByUserId.TryGetValue(habbo.Id, out var guideSession))
                return Task.CompletedTask;

            int reportedUserId = guideSession.RequesterId == habbo.Id
                ? guideSession.HelperId ?? 0
                : guideSession.RequesterId;
            if (reportedUserId == 0 || !TryGetClientLocked(reportedUserId, out var reportedClient))
            {
                RemoveSessionLocked(guideSession, GuideSessionEndedComposer.HelpCaseClosed);
                return Task.CompletedTask;
            }

            var reportedHabbo = reportedClient.GetHabbo();
            if (habbo.TryGetCurrentRoom(out var room))
            {
                var ticket = new ModerationTicket(
                    0,
                    1,
                    0,
                    UnixTimestamp.GetNow(),
                    1,
                    habbo,
                    reportedHabbo,
                    trimmedMessage,
                    room,
                    guideSession.Messages.Select(chat => chat.Message).ToList());
                _moderationManager.TryAddTicket(ticket);
                _clientManager.SendPacket(new ModeratorSupportTicketComposer(habbo.Id, ticket), "mod_tool");
            }

            session.SendNotification("Guide report submitted.");
            RemoveSessionLocked(guideSession, GuideSessionEndedComposer.HelpCaseClosed);
        }

        return Task.CompletedTask;
    }

    private void WatchDisconnect(Users.Habbo habbo)
    {
        if (!_watchedUsers.TryAdd(habbo.Id, 0))
            return;

        habbo.Disconnected += OnHabboDisconnected;
    }

    private void OnHabboDisconnected(object? sender, EventArgs e)
    {
        if (sender is not Users.Habbo habbo)
            return;

        habbo.Disconnected -= OnHabboDisconnected;
        _watchedUsers.TryRemove(habbo.Id, out _);

        lock (_sync)
        {
            if (_sessionsByUserId.TryGetValue(habbo.Id, out var guideSession))
                RemoveSessionLocked(guideSession, GuideSessionEndedComposer.HelpCaseClosed);
            else
                _helpers.Remove(habbo.Id);
        }
    }

    private bool TryAttachNextHelperLocked(GuideSession guideSession)
    {
        foreach (var helperEntry in _helpers.OrderBy(entry => entry.Key).ToList())
        {
            if (helperEntry.Value || guideSession.DeclinedHelperIds.Contains(helperEntry.Key))
                continue;
            if (!TryGetClientLocked(helperEntry.Key, out var helperClient) || !TryGetClientLocked(guideSession.RequesterId, out var requesterClient))
                continue;

            guideSession.HelperId = helperEntry.Key;
            _sessionsByUserId[guideSession.RequesterId] = guideSession;
            _sessionsByUserId[helperEntry.Key] = guideSession;

            helperClient.Send(new GuideSessionAttachedComposer(isHelper: true, guideSession.Message, 60));
            requesterClient.Send(new GuideSessionAttachedComposer(isHelper: false, guideSession.Message, GetAverageWaitingTimeLocked()));
            return true;
        }

        return false;
    }

    private int GetAverageWaitingTimeLocked() => _resolvedWaitingTimes.Count == 0 ? 5 : (int)_resolvedWaitingTimes.Average();

    private void RemoveSessionLocked(
        GuideSession guideSession,
        int endReason,
        bool detachRequester = true,
        bool detachHelper = true)
    {
        _sessionsByUserId.Remove(guideSession.RequesterId);

        if (TryGetClientLocked(guideSession.RequesterId, out var requester))
        {
            requester.Send(new GuideSessionEndedComposer(endReason));
            if (detachRequester)
                requester.Send(new GuideSessionDetachedComposer());
        }

        if (guideSession.HelperId is not { } helperId)
            return;

        _sessionsByUserId.Remove(helperId);
        _helpers[helperId] = false;

        if (!TryGetClientLocked(helperId, out var helper))
            return;

        helper.Send(new GuideSessionEndedComposer(endReason));
        if (detachHelper)
            helper.Send(new GuideSessionDetachedComposer());
        helper.Send(new HelperToolComposer(true, _helpers.Count, _guardianService.GuardiansOnDuty));
    }

    private bool TryGetClientLocked(int userId, out GameClient client)
    {
        client = _clientManager.GetClientByUserId(userId)!;
        return client?.GetHabboOrNull() != null;
    }

    private void PruneLocked()
    {
        foreach (var helperId in _helpers.Keys.ToList())
        {
            if (_clientManager.GetClientByUserId(helperId)?.GetHabboOrNull() != null)
                continue;

            _helpers.Remove(helperId);
        }

        foreach (var session in _sessionsByUserId.Values.Distinct().ToList())
        {
            bool requesterOnline = _clientManager.GetClientByUserId(session.RequesterId)?.GetHabboOrNull() != null;
            bool helperOnline = session.HelperId is not { } helperId || _clientManager.GetClientByUserId(helperId)?.GetHabboOrNull() != null;

            if (requesterOnline && helperOnline)
                continue;

            RemoveSessionLocked(session, GuideSessionEndedComposer.HelpCaseClosed);
        }
    }
}
