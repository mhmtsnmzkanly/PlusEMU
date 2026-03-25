using Microsoft.Extensions.Logging;
using Plus.Communication.Attributes;
using Plus.Communication.Encryption;
using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Handshake;

[NoAuthenticationRequired]
public class InitDiffieHandshakeEvent : IPacketEvent
{
    private readonly ILogger<InitDiffieHandshakeEvent> _logger;

    public InitDiffieHandshakeEvent(ILogger<InitDiffieHandshakeEvent> logger)
    {
        _logger = logger;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        _logger.LogInformation("Received InitDiffieHandshakeEvent for session {sessionId}. Build: {build}.", session.Id, session.ClientBuild ?? "<unknown>");
        session.Send(new InitCryptoComposer(HabboEncryptionV2.GetRsaDiffieHellmanPrimeKey(), HabboEncryptionV2.GetRsaDiffieHellmanGeneratorKey()));
        return Task.CompletedTask;
    }
}
