using Microsoft.Extensions.Logging;
using Plus.Communication.Attributes;
using Plus.Communication.Revisions;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Handshake;

[NoAuthenticationRequired]
public class ClientHelloEvent : IPacketEvent
{
    private readonly IRevisionsCache _revisionsCache;
    private readonly ILogger _logger;

    public ClientHelloEvent(IRevisionsCache revisionsCache, ILogger<ClientHelloEvent> logger)
    {
        _revisionsCache = revisionsCache;
        _logger = logger;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var build = packet.ReadString();
        packet.ReadString();
        packet.ReadInt();
        packet.ReadInt();
        session.ClientBuild = build;
        if (!_revisionsCache.Revisions.TryGetValue(build, out var revision))
        {
            _logger.LogWarning("Unknown revision connected. Session {sessionId}, revision {revision}. Loaded revisions: {loadedRevisions}.",
                session.Id,
                build,
                string.Join(", ", _revisionsCache.Revisions.Keys.OrderBy(x => x)));
            session.Disconnect($"Unknown revision: {build}");
            return Task.CompletedTask;
        }

        session.Revision = revision;
        _logger.LogInformation("Accepted client hello for session {sessionId}. Revision {revision}.", session.Id, build);
        return Task.CompletedTask;
    }
}
