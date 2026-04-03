using System.Net;
using System.Net.Sockets;
using Plus.Communication.RCON.Commands;

namespace Plus.Communication.RCON;

    public class RconSocket : IRconSocket
{
    private List<string> _allowedConnections = new();
    private readonly IRconCommandManagerAccessor _commandManagerAccessor;
    private ICommandManager? _commands;
    private Socket? _musSocket;

    public RconSocket(IRconCommandManagerAccessor commandManagerAccessor)
    {
        _commandManagerAccessor = commandManagerAccessor;
    }

    public void Init(string host, int port, IEnumerable<string> allowedConnections)
    {
        _allowedConnections = new();
        foreach (var ipAddress in allowedConnections) _allowedConnections.Add(ipAddress);
        try
        {
            _musSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _musSocket.Bind(new IPEndPoint(IPAddress.Parse(host), port)); // SHould be host?
            _musSocket.Listen(0);
            _musSocket.BeginAccept(OnCallBack, _musSocket);
        }
        catch (Exception e)
        {
            throw new ArgumentException($"Could not set up Rcon socket:\n{e}");
        }
    }

    public void Stop()
    {
        try
        {
            _musSocket?.Close();
            _musSocket?.Dispose();
        }
        catch
        {
            // ignored
        }
        finally
        {
            _musSocket = null;
        }
    }

    private void OnCallBack(IAsyncResult iAr)
    {
        try
        {
            if (iAr.AsyncState is not Socket listenSocket)
                return;

            var socket = listenSocket.EndAccept(iAr);
            var remoteEndPoint = socket.RemoteEndPoint?.ToString();
            if (string.IsNullOrEmpty(remoteEndPoint))
            {
                socket.Close();
                return;
            }

            var ip = remoteEndPoint.Split(':')[0];
            if (_allowedConnections.Contains(ip))
                new RconConnection(socket, GetCommands());
            else
                socket.Close();
        }
        catch (Exception)
        {
            // ignored
        }
        if (_musSocket != null)
            _musSocket.BeginAccept(OnCallBack, _musSocket);
    }

    public ICommandManager GetCommands() => _commands ??= _commandManagerAccessor.Get();
}
