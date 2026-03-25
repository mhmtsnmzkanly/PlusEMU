using System.Net.Sockets;

namespace Plus.HabboHotel.GameClients;

internal static class SocketLogging
{
    public static string? TryGetRemoteEndPoint(Socket? socket)
    {
        if (socket == null)
            return null;

        try
        {
            return socket.RemoteEndPoint?.ToString();
        }
        catch (ObjectDisposedException)
        {
            return "<disposed>";
        }
        catch (SocketException)
        {
            return "<socket-error>";
        }
    }
}
