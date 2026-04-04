using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuardianVotingResultComposer : IServerPacket
{
    private readonly GuardianTicket _ticket;
    private readonly GuardianVote _vote;

    public GuardianVotingResultComposer(GuardianTicket ticket, GuardianVote vote)
    {
        _ticket = ticket;
        _vote = vote;
    }

    public uint MessageId => ServerPacketHeader.GuardianVotingResultComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger((int)(_ticket.Verdict ?? GuardianVoteType.Forwarded));
        packet.WriteInteger((int)_vote.Type);

        var otherVotes = _ticket.Votes.Values
            .Where(v => v.UserId != _vote.UserId && !v.Ignored && v.Type is GuardianVoteType.Acceptably or GuardianVoteType.Badly or GuardianVoteType.Awfully)
            .Select(v => (int)v.Type)
            .ToList();

        packet.WriteInteger(otherVotes.Count);
        foreach (var vote in otherVotes)
            packet.WriteInteger(vote);
    }
}
