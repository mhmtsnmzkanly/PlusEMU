using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCoreServer;
using Plus.Communication.Packets;
using Plus.HabboHotel.GameClients;
using System.Collections.Concurrent;
using Plus.Communication.Flash;

namespace Plus.Communication.Abstractions;

public abstract class TcpGameServer<TGameServerOptions> : TcpServer, IGameServer
    where TGameServerOptions : class, IGameServerOptions
{
    private readonly IGameClientFactory<TcpSessionProxy, TcpServer> _clientFactory;
    private readonly IPacketManager _packetManager;
    private readonly ConcurrentDictionary<Guid, TcpSession> _connectedClients = new();
    private readonly ILogger _logger;

    protected TcpGameServer(IOptions<TGameServerOptions> options,
        IGameClientFactory<TcpSessionProxy, TcpServer> clientFactory,
        IPacketManager packetManager,
        ILogger logger) : base(options.Value.Hostname,
        options.Value.Port)
    {
        _clientFactory = clientFactory;
        _packetManager = packetManager;
        _logger = logger;
    }

    protected override TcpSession CreateSession() => _clientFactory.Create(this);

    protected override void OnConnected(TcpSession session)
    {
        if (session is not TcpSessionProxy gameClient)
        {
            session.Disconnect();
            _logger.LogWarning("Rejected TCP session {sessionId}: unexpected session type {type}.", session.Id, session.GetType().Name);
            return;
        }

        if (!_connectedClients.TryAdd(gameClient.Id, gameClient))
        {
            _logger.LogWarning("Failed to cache TCP client {clientId} from {remoteEndPoint}.", gameClient.Id, SocketLogging.TryGetRemoteEndPoint(gameClient.Socket));
            gameClient.Disconnect();
            return;
        }

        _logger.LogDebug("TCP client connected {clientId} from {remoteEndPoint}.", gameClient.Id, SocketLogging.TryGetRemoteEndPoint(gameClient.Socket));
    }

    protected override void OnDisconnected(TcpSession session)
    {
        _connectedClients.TryRemove(session.Id, out _);
        _logger.LogDebug("TCP client disconnected {clientId} from {remoteEndPoint}.", session.Id, SocketLogging.TryGetRemoteEndPoint(session.Socket));
    }

    protected override void OnError(System.Net.Sockets.SocketError error)
    {
        _logger.LogError("TCP server socket error: {error}.", error);
    }

    // TODO @80O: Allow packet content to be modified before executing.
    // TODO @80O: Add hooks before & after packet execution.
    public Task PacketReceived(GameClient client, uint messageId, IIncomingPacket packet) => _packetManager.TryExecutePacket(client, messageId, packet);
}

public interface IGameClientFactory<TGameClient, TServer> : IGameClientFactory
{
    TGameClient Create(TServer server);
}
