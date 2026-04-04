using System.Text;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Outgoing.Guides;

public sealed class GuardianVotingRequestedComposer : IServerPacket
{
    private readonly GuardianTicket _ticket;

    public GuardianVotingRequestedComposer(GuardianTicket ticket) => _ticket = ticket;

    public uint MessageId => ServerPacketHeader.GuardianVotingRequestedComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_ticket.TimeLeftSeconds);

        var createdAt = DateTimeOffset.FromUnixTimeSeconds(_ticket.CreatedAt);
        var transcript = new StringBuilder();
        transcript.Append(createdAt.Year).Append(' ')
            .Append(createdAt.Month).Append(' ')
            .Append(createdAt.Day).Append(' ')
            .Append(createdAt.Minute).Append(' ')
            .Append(createdAt.Second).Append(';')
            .Append("\r");

        foreach (var line in _ticket.ChatLog)
            transcript.Append("unused;0;").Append(line).Append("\r");

        packet.WriteString(transcript.ToString());
    }
}
