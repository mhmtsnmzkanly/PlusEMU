using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using NetCoreServer;
using Plus.Communication.Packets;
using System.Collections.Concurrent;
using Plus.Communication.Flash;

namespace Plus.Communication.Abstractions;

public abstract class TcpGameServer<TGameServerOptions> : TcpServer, IGameServer
    where TGameServerOptions : class, IGameServerOptions
{
    private readonly IGameClientFactory<TcpSessionProxy, TcpServer> _clientFactory;
    private readonly IPacketManager _packetManager;
    private readonly ConcurrentDictionary<Guid, TcpSession> _connectedClients = new();
    private readonly IGameClientManager _clientManager;
    private readonly IDatabase _database;
    private readonly ILogger _logger;

    protected TcpGameServer(IOptions<TGameServerOptions> options,
        IGameClientFactory<TcpSessionProxy, TcpServer> clientFactory,
        IPacketManager packetManager,
        IGameClientManager clientManager,
        IDatabase database,
        ILogger logger) : base(options.Value.Hostname,
        options.Value.Port)
    {
        _clientFactory = clientFactory;
        _packetManager = packetManager;
        _clientManager = clientManager;
        _database = database;
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
        if (session is TcpSessionProxy proxy)
        {
            var habbo = proxy.Client.OnDisconnected();
            if (habbo != null)
                _clientManager.UnregisterClient(habbo.Id, habbo.Username);
        }
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
