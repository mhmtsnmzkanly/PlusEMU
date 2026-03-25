using Microsoft.Extensions.Logging;
using Plus.Communication.Attributes;
using Plus.Communication.Encryption;
using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Handshake;

[NoAuthenticationRequired]
public class GenerateSecretKeyEvent : IPacketEvent
{
    private readonly ILogger<GenerateSecretKeyEvent> _logger;

    public GenerateSecretKeyEvent(ILogger<GenerateSecretKeyEvent> logger)
    {
        _logger = logger;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        _logger.LogInformation("Received GenerateSecretKeyEvent for session {sessionId}. Build: {build}.", session.Id, session.ClientBuild ?? "<unknown>");
        var cipherPublickey = packet.ReadString();
        var sharedKey = HabboEncryptionV2.CalculateDiffieHellmanSharedKey(cipherPublickey);
        if (sharedKey != 0)
        {
            session.Rc4Client = new(sharedKey.getBytes());
            session.Send(new SecretKeyComposer(HabboEncryptionV2.GetRsaDiffieHellmanPublicKey()));
            _logger.LogInformation("Generated RC4 secret for session {sessionId}.", session.Id);
        }
        else
        {
            _logger.LogWarning("Failed to calculate shared key for session {sessionId}.", session.Id);
            session.SendNotification("There was an error logging you in, please try again!");
        }
        return Task.CompletedTask;
    }
}
