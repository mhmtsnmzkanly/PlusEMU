using Microsoft.Extensions.Logging;
using NetCoreServer;
using System.Net.Sockets;

namespace Plus.HabboHotel.GameClients;

public class TcpSessionProxy : TcpSession
{
    private readonly GameClient _client;
    private readonly ILogger<TcpSessionProxy> _logger;

    public GameClient Client => _client;

    public TcpSessionProxy(TcpServer server, GameClient client, ILogger<TcpSessionProxy> logger) : base(server)
    {
        _client = client;
        _logger = logger;
        _client.Id = Id;
        _client.SendCallback = args =>
        {
            if (!Socket.Connected) return false;
            try
            {
                return Socket.SendAsync(args);
            }
            catch (Exception) // TODO 80O: Maybe handle some potential errors.
            {
            }
            return false;
        };
        _client.DisconnectRequested = reason =>
        {
            _logger.LogWarning("TCP session {sessionId} disconnect requested from {remoteEndPoint}. Reason: {reason}.", Id, SocketLogging.TryGetRemoteEndPoint(Socket), string.IsNullOrWhiteSpace(reason) ? "Unspecified" : reason);
            Disconnect();
        };
    }

    protected override void OnConnected()
    {
        base.OnConnected();
    }

    protected override void OnDisconnected() => _client.OnDisconnected();

    protected override void OnReceived(byte[] buffer, long offset, long size) => _client.OnReceived(buffer, offset, size);

    protected override void OnError(SocketError error)
    {
        _logger.LogError("TCP session {sessionId} socket error from {remoteEndPoint}: {error}.", Id, SocketLogging.TryGetRemoteEndPoint(Socket), error);
    }
}
