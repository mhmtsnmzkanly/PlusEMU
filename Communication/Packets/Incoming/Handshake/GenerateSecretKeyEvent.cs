using Microsoft.Extensions.Logging;
using Plus.Communication.Attributes;
using Plus.Communication.Encryption;
using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.Core.Language;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Handshake;

[NoAuthenticationRequired]
public class GenerateSecretKeyEvent : IPacketEvent
{
    private readonly ILogger<GenerateSecretKeyEvent> _logger;
    private readonly ILanguageManager _languageManager;

    public GenerateSecretKeyEvent(ILogger<GenerateSecretKeyEvent> logger, ILanguageManager languageManager)
    {
        _logger = logger;
        _languageManager = languageManager;
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
            session.SendNotification(_languageManager.Require("auth.secret_key.failed"));
        }
        return Task.CompletedTask;
    }
}
