using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuardianVotingVotesComposer : IServerPacket
{
    private readonly GuardianTicket _ticket;
    private readonly int _guardianUserId;

    public GuardianVotingVotesComposer(GuardianTicket ticket, int guardianUserId)
    {
        _ticket = ticket;
        _guardianUserId = guardianUserId;
    }

    public uint MessageId => ServerPacketHeader.GuardianVotingVotesComposer;

    public void Compose(IOutgoingPacket packet)
    {
        var votes = _ticket.Votes.Values
            .Where(v => v.UserId != _guardianUserId && !v.Ignored && v.Type is GuardianVoteType.Acceptably or GuardianVoteType.Badly or GuardianVoteType.Awfully)
            .Select(v => (int)v.Type)
            .ToList();

        packet.WriteInteger(votes.Count);
        foreach (var vote in votes)
            packet.WriteInteger(vote);
    }
}
