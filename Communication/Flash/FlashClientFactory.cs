using Microsoft.Extensions.Logging;
using NetCoreServer;
using Plus.Communication.Abstractions;
using Plus.Communication.Revisions;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Flash;

public class FlashClientFactory : IGameClientFactory<TcpSessionProxy, TcpServer>
{
    private readonly FlashPacketFactory _packetFactory;
    private readonly IRevisionsCache _revisionsCache;
    private readonly ILogger<TcpSessionProxy> _sessionLogger;

    public FlashClientFactory(FlashPacketFactory packetFactory, IRevisionsCache revisionsCache, ILogger<TcpSessionProxy> sessionLogger)
    {
        _packetFactory = packetFactory;
        _revisionsCache = revisionsCache;
        _sessionLogger = sessionLogger;
    }

    public TcpSessionProxy Create(TcpServer server) => new((FlashServer)server, new FlashGameClient((FlashServer)server, _packetFactory) { Revision = _revisionsCache.InternalRevision }, _sessionLogger);
}
