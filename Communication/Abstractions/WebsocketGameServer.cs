using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Microsoft.Extensions.Options;
using NetCoreServer;
using Plus.Communication.Flash;
using Plus.Communication.Packets;

namespace Plus.Communication.Abstractions;

public abstract class WebsocketGameServer<TGameServerOptions> : WsServer, IGameServer
    where TGameServerOptions : class, IGameServerOptions
{
    private readonly IGameClientFactory<WsSessionProxy, WsServer> _clientFactory;
    private readonly IPacketManager _packetManager;
    private readonly ConcurrentDictionary<Guid, WsSessionProxy> _connectedClients = new();
    private readonly IGameClientManager _clientManager;
    private readonly IDatabase _database;
    private readonly ILogger _logger;

    protected WebsocketGameServer(IOptions<TGameServerOptions> options,
        IGameClientFactory<WsSessionProxy, WsServer> clientFactory,
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

    protected override WsSession CreateSession() => _clientFactory.Create(this);

    protected override void OnConnected(TcpSession session)
    {
        if (session is not WsSessionProxy gameClient)
        {
            session.Disconnect();
            _logger.LogWarning("Rejected websocket session {sessionId}: unexpected session type {type}.", session.Id, session.GetType().Name);
            return;
        }

        if (!_connectedClients.TryAdd(gameClient.Id, gameClient))
        {
            _logger.LogWarning("Failed to cache websocket client {clientId} from {remoteEndPoint}.", gameClient.Id, SocketLogging.TryGetRemoteEndPoint(gameClient.Socket));
            gameClient.Disconnect();
            return;
        }

        _logger.LogDebug("Websocket client connected {clientId} from {remoteEndPoint}. Build: {build}.", gameClient.Id, SocketLogging.TryGetRemoteEndPoint(gameClient.Socket), gameClient.GetClientBuildDisplay());
    }

    protected override void OnDisconnected(TcpSession session)
    {
        if (session is WsSessionProxy gameClient)
        {
            var habbo = gameClient.Client.OnDisconnected();
            if (habbo != null)
                _clientManager.UnregisterClient(habbo.Id, habbo.Username);
            _logger.LogDebug("Websocket client disconnected {clientId} from {remoteEndPoint}. Build: {build}.",
                session.Id,
                SocketLogging.TryGetRemoteEndPoint(session.Socket),
                gameClient.GetClientBuildDisplay());
        }
        else
            _logger.LogDebug("Websocket client disconnected {clientId} from {remoteEndPoint}.", session.Id, SocketLogging.TryGetRemoteEndPoint(session.Socket));
        _connectedClients.TryRemove(session.Id, out _);
    }

    protected override void OnError(System.Net.Sockets.SocketError error)
    {
        _logger.LogError("Websocket server socket error: {error}.", error);
    }


    // TODO @80O: Allow packet content to be modified before executing.
    // TODO @80O: Add hooks before & after packet execution.
    public Task PacketReceived(GameClient client, uint messageId, IIncomingPacket packet) => _packetManager.TryExecutePacket(client, messageId, packet);
}
