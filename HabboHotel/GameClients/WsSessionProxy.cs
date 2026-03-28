using Microsoft.Extensions.Logging;
using NetCoreServer;
using System.Net.Sockets;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.GameClients;

public class WsSessionProxy : WsSession
{
    private readonly GameClient _client;
    private readonly ILogger<WsSessionProxy> _logger;

    public GameClient Client => _client;
    public Habbo? GetHabboOrNull() => _client.GetHabboOrNull();

    public WsSessionProxy(WsServer server, GameClient client, ILogger<WsSessionProxy> logger) : base(server)
    {
        _client = client;
        _logger = logger;
        _client.Id = Id;
        _client.SendCallback = args =>
        {
            if (!Socket.Connected) return false;
            var buffer = args.MemoryBuffer.ToArray();
            return SendBinaryAsync(buffer, 0, buffer.Length);
        };
        _client.DisconnectRequested = reason =>
        {
            _logger.LogWarning("Websocket session {sessionId} disconnect requested from {remoteEndPoint}. Reason: {reason}. Build: {build}.",
                Id,
                SocketLogging.TryGetRemoteEndPoint(Socket),
                string.IsNullOrWhiteSpace(reason) ? "Unspecified" : reason,
                _client.ClientBuild ?? "<unknown>");
            Disconnect();
        };
    }

    protected override void OnConnected()
    {
        base.OnConnected();
    }

    protected override void OnDisconnected()
    {
    }

    public override void OnWsReceived(byte[] buffer, long offset, long size) => _client.OnReceived(buffer, offset, size);

    protected override void OnError(SocketError error)
    {
        _logger.LogError("Websocket session {sessionId} socket error from {remoteEndPoint}: {error}.", Id, SocketLogging.TryGetRemoteEndPoint(Socket), error);
    }

    public string GetClientBuildDisplay() => _client.ClientBuild ?? "<pending-client-hello>";
}
