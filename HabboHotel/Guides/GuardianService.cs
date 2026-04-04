using Dapper;
using Plus.Communication.Packets.Outgoing.Guides;
using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Guides;

internal sealed class GuardianService : IGuardianService
{
    private const string GuideToolPermission = "mod_tool";
    private const int AcceptTimerSeconds = 30;
    private const int VotingTimerSeconds = 120;
    private const int MinimumVotes = 3;
    private const int MaxAssignments = 5;
    private const int MaxResends = 2;

    private readonly object _sync = new();
    private readonly IDatabase _database;
    private readonly IGameClientManager _clientManager;
    private readonly Dictionary<int, bool> _guardians = new();
    private readonly Dictionary<int, GuardianTicket> _ticketsByGuardian = new();
    private readonly Dictionary<int, GuardianTicket> _ticketsByReported = new();

    public GuardianService(IDatabase database, IGameClientManager clientManager)
    {
        _database = database;
        _clientManager = clientManager;
    }

    public int GuardiansOnDuty
    {
        get
        {
            lock (_sync)
                return _guardians.Count;
        }
    }

    public Task SetOnDuty(GameClient session, bool onDuty)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo?.Permissions == null)
            return Task.CompletedTask;

        if (!habbo.Permissions.HasRight(GuideToolPermission) && !habbo.IsAmbassador)
            return Task.CompletedTask;

        lock (_sync)
        {
            if (!onDuty)
            {
                _guardians.Remove(habbo.Id);
                if (_ticketsByGuardian.TryGetValue(habbo.Id, out var ticket))
                {
                    if (ticket.Votes.TryGetValue(habbo.Id, out var vote))
                        vote.Ignored = true;
                    _ticketsByGuardian.Remove(habbo.Id);
                }

                return Task.CompletedTask;
            }

            _guardians[habbo.Id] = false;
        }

        return Task.CompletedTask;
    }

    public async Task<bool> SubmitReport(GameClient reporterSession, GameClient reportedSession)
    {
        var reporter = reporterSession.GetHabboOrNull();
        var reported = reportedSession.GetHabboOrNull();
        if (reporter == null || reported == null)
            return false;

        IReadOnlyList<string> chatLog;
        using (var connection = _database.Connection())
        {
            chatLog = (await connection.QueryAsync<string>(
                "SELECT `message` FROM `chatlogs` WHERE `user_id` = @userId ORDER BY `id` DESC LIMIT 10",
                new { userId = reported.Id })).Reverse().ToList();
        }

        if (chatLog.Count == 0)
            return false;

        GuardianTicket ticket;
        lock (_sync)
        {
            if (_ticketsByReported.ContainsKey(reported.Id))
                return true;

            ticket = new GuardianTicket(reporter.Id, reported.Id, chatLog, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                TimeLeftSeconds = VotingTimerSeconds
            };

            if (!AssignMoreGuardiansLocked(ticket))
                return false;
            _ticketsByReported[reported.Id] = ticket;
        }

        ScheduleFinalize(ticket);
        return true;
    }

    public Task AcceptTicket(GameClient session, bool accepted)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        lock (_sync)
        {
            if (!_ticketsByGuardian.TryGetValue(habbo.Id, out var ticket) || !ticket.Votes.TryGetValue(habbo.Id, out var vote))
                return Task.CompletedTask;

            if (!accepted)
            {
                ReleaseGuardianLocked(ticket, habbo.Id, ignored: true);
                TryResendLocked(ticket);
                return Task.CompletedTask;
            }

            vote.Type = GuardianVoteType.Waiting;
            session.Send(new GuardianVotingRequestedComposer(ticket));
            UpdateVoteCounts(ticket);
        }

        return Task.CompletedTask;
    }

    public Task Vote(GameClient session, int voteType)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        lock (_sync)
        {
            if (!_ticketsByGuardian.TryGetValue(habbo.Id, out var ticket) || !ticket.Votes.TryGetValue(habbo.Id, out var vote))
                return Task.CompletedTask;

            vote.Type = voteType switch
            {
                0 => GuardianVoteType.Acceptably,
                1 => GuardianVoteType.Badly,
                2 => GuardianVoteType.Awfully,
                _ => GuardianVoteType.NotVoted
            };

            UpdateVoteCounts(ticket);
            if (GetCastVotesLocked(ticket) >= MinimumVotes)
                CloseTicket(ticket);
        }

        return Task.CompletedTask;
    }

    public Task IgnoreUpdates(GameClient session)
    {
        var habbo = session.GetHabboOrNull();
        if (habbo == null)
            return Task.CompletedTask;

        lock (_sync)
        {
            if (_ticketsByGuardian.TryGetValue(habbo.Id, out var ticket) && ticket.Votes.TryGetValue(habbo.Id, out var vote))
                vote.Ignored = true;
        }

        return Task.CompletedTask;
    }

    private void NotifyGuardians()
    {
        foreach (var guardianId in _guardians.Keys.ToList())
        {
            if (TryGetGuardianClient(guardianId, out var guardianClient))
                guardianClient.Send(new GuardianNewReportReceivedComposer(AcceptTimerSeconds));
        }
    }

    private bool AssignMoreGuardiansLocked(GuardianTicket ticket)
    {
        int assignedCount = ticket.Votes.Count;
        foreach (var guardianId in _guardians
                     .Where(entry => !entry.Value)
                     .Select(entry => entry.Key)
                     .ToList())
        {
            if (assignedCount >= MaxAssignments)
                break;
            if (guardianId == ticket.ReporterId || guardianId == ticket.ReportedId)
                continue;
            if (ticket.Votes.ContainsKey(guardianId))
                continue;
            if (!TryGetGuardianClient(guardianId, out var guardianClient))
                continue;

            ticket.Votes[guardianId] = new GuardianVote(guardianId);
            _ticketsByGuardian[guardianId] = ticket;
            _guardians[guardianId] = true;
            guardianClient.Send(new GuardianNewReportReceivedComposer(AcceptTimerSeconds));
            ScheduleGuardianAcceptTimeout(ticket, guardianId);
            assignedCount++;
        }

        return ticket.Votes.Count > 0;
    }

    private void ScheduleGuardianAcceptTimeout(GuardianTicket ticket, int guardianId)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(AcceptTimerSeconds));
            lock (_sync)
            {
                if (ticket.Closed || !_ticketsByReported.ContainsKey(ticket.ReportedId))
                    return;
                if (!ticket.Votes.TryGetValue(guardianId, out var vote) || vote.Type != GuardianVoteType.Searching)
                    return;

                ReleaseGuardianLocked(ticket, guardianId, ignored: true);
                TryResendLocked(ticket);
            }
        });
    }

    private void ScheduleFinalize(GuardianTicket ticket)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(VotingTimerSeconds));
            lock (_sync)
            {
                if (ticket.Closed || !_ticketsByReported.ContainsKey(ticket.ReportedId))
                    return;

                if (GetCastVotesLocked(ticket) >= MinimumVotes)
                {
                    CloseTicket(ticket);
                    return;
                }

                if (TryResendLocked(ticket))
                {
                    ScheduleFinalize(ticket);
                    return;
                }

                ForwardToModerationLocked(ticket);
            }
        });
    }

    private bool TryResendLocked(GuardianTicket ticket)
    {
        if (ticket.Closed)
            return false;
        if (GetCastVotesLocked(ticket) >= MinimumVotes)
        {
            CloseTicket(ticket);
            return false;
        }
        if (ticket.ResendCount >= MaxResends)
            return false;

        int assignedBefore = ticket.Votes.Count;
        ticket.ResendCount++;
        return AssignMoreGuardiansLocked(ticket) && ticket.Votes.Count > assignedBefore;
    }

    private void ReleaseGuardianLocked(GuardianTicket ticket, int guardianId, bool ignored)
    {
        if (ticket.Votes.TryGetValue(guardianId, out var vote))
        {
            vote.Type = GuardianVoteType.NotVoted;
            vote.Ignored = ignored;
        }

        _ticketsByGuardian.Remove(guardianId);
        if (_guardians.ContainsKey(guardianId))
            _guardians[guardianId] = false;
    }

    private void UpdateVoteCounts(GuardianTicket ticket)
    {
        foreach (var entry in ticket.Votes)
        {
            if (entry.Value.Ignored || entry.Value.Type is GuardianVoteType.Searching or GuardianVoteType.NotVoted)
                continue;

            if (TryGetGuardianClient(entry.Key, out var guardianClient))
                guardianClient.Send(new GuardianVotingVotesComposer(ticket, entry.Key));
        }
    }

    private void CloseTicket(GuardianTicket ticket)
    {
        if (ticket.Closed)
            return;

        ticket.Closed = true;
        ticket.Verdict = CalculateVerdict(ticket);

        foreach (var entry in ticket.Votes)
        {
            if (TryGetGuardianClient(entry.Key, out var guardianClient)
                && entry.Value.Type is GuardianVoteType.Acceptably or GuardianVoteType.Badly or GuardianVoteType.Awfully)
                guardianClient.Send(new GuardianVotingResultComposer(ticket, entry.Value));

            _guardians[entry.Key] = false;
            _ticketsByGuardian.Remove(entry.Key);
        }

        _ticketsByReported.Remove(ticket.ReportedId);
    }

    private void ForwardToModerationLocked(GuardianTicket ticket)
    {
        if (ticket.Closed)
            return;

        ticket.Closed = true;
        ticket.Verdict = GuardianVoteType.Forwarded;

        foreach (var guardianId in ticket.Votes.Keys.ToList())
        {
            _ticketsByGuardian.Remove(guardianId);
            if (_guardians.ContainsKey(guardianId))
                _guardians[guardianId] = false;
        }

        _ticketsByReported.Remove(ticket.ReportedId);

        var reporterClient = _clientManager.GetClientByUserId(ticket.ReporterId);
        var reportedClient = _clientManager.GetClientByUserId(ticket.ReportedId);
        if (reporterClient != null && reportedClient != null)
            _clientManager.DoAdvertisingReport(reporterClient, reportedClient);
        else
            _clientManager.ModAlert($"Guardian review forwarded to moderators for reported user {ticket.ReportedId}.");
    }

    private static GuardianVoteType CalculateVerdict(GuardianTicket ticket)
    {
        int acceptably = ticket.Votes.Values.Count(v => v.Type == GuardianVoteType.Acceptably);
        int badly = ticket.Votes.Values.Count(v => v.Type == GuardianVoteType.Badly);
        int awfully = ticket.Votes.Values.Count(v => v.Type == GuardianVoteType.Awfully);

        if (awfully >= badly && awfully >= acceptably)
            return GuardianVoteType.Awfully;
        if (badly >= acceptably)
            return GuardianVoteType.Badly;
        return GuardianVoteType.Acceptably;
    }

    private bool TryGetGuardianClient(int guardianId, out GameClient client)
    {
        client = _clientManager.GetClientByUserId(guardianId)!;
        return client?.GetHabboOrNull() != null;
    }

    private static int GetCastVotesLocked(GuardianTicket ticket) =>
        ticket.Votes.Values.Count(v => v.Type is GuardianVoteType.Acceptably or GuardianVoteType.Badly or GuardianVoteType.Awfully);
}
