using Microsoft.Extensions.Logging;
using Plus.Communication.Attributes;
using Plus.Communication.Packets.Incoming;
using Plus.HabboHotel.GameClients;
using System.Diagnostics;
using System.Reflection;

namespace Plus.Communication.Packets;

public sealed class PacketManager : IPacketManager, IDisposable
{
    private readonly ILogger<PacketManager> _logger;
    private readonly IPacketEventActivator _packetEventActivator;

    private readonly Dictionary<uint, Type> _incomingPackets = new();
    private readonly HashSet<Type> _handshakePackets = new();
    private readonly Dictionary<uint, string> _packetNames = new();

    /// <summary>
    ///     The maximum time a task can run for before it is considered dead
    ///     (can be used for debugging any locking issues with certain areas of code)
    /// </summary>
    private readonly TimeSpan _maximumRunTimeInSec; // 5 minutes in debug. 30 seconds in release.
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public PacketManager(IPacketEventActivator packetEventActivator, ILogger<PacketManager> logger)
    {
        _maximumRunTimeInSec = Debugger.IsAttached ? TimeSpan.FromMinutes(30) : TimeSpan.FromSeconds(5);
        _packetEventActivator = packetEventActivator;
        _logger = logger;

        var packetTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    return e.Types.OfType<Type>();
                }
            })
            .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IPacketEvent).IsAssignableFrom(type));

        foreach (var packetType in packetTypes)
        {
            var field = typeof(ClientPacketHeader).GetField(packetType.Name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (field == null)
            {
                _logger.LogWarning("No incoming header defined for {packet}", packetType.Name);
                continue;
            }
            var header = (uint)field.GetValue(null)!;
            _incomingPackets[header] = packetType;
            _packetNames[header] = packetType.Name;
            if (packetType.GetCustomAttribute<NoAuthenticationRequiredAttribute>() != null)
                _handshakePackets.Add(packetType);
        }
    }

    public async Task TryExecutePacket(GameClient session, uint messageId, IIncomingPacket packet)
    {
        if (!_incomingPackets.TryGetValue(messageId, out var packetType))
        {
            _logger.LogWarning("Unhandled packet {messageId} for session {sessionId}. Build: {build}.", messageId, session.Id, session.ClientBuild ?? "<unknown>");
            return;
        }

        if (Debugger.IsAttached)
        {
            if (_packetNames.ContainsKey(messageId))
                _logger.LogDebug("Handled Packet: [" + messageId + "] " + _packetNames[messageId]);
            else
                _logger.LogDebug("Handled Packet: [" + messageId + "] UnnamedPacketEvent");
        }

        if (!_handshakePackets.Contains(packetType) && session.GetHabbo() == null)
        {
            _logger.LogDebug($"Session {session.Id} tried execute packet {messageId} but didn't handshake yet.");
            return;
        }

        if (!_packetEventActivator.TryActivate(packetType, out var pak))
        {
            _logger.LogError("Failed to resolve packet handler {packetType} for message {messageId}.", packetType.FullName, messageId);
            return;
        }

        await ExecutePacketAsync(session, packet, pak);
    }

    private async Task ExecutePacketAsync(GameClient session, IIncomingPacket packet, IPacketEvent pak)
    {
        if (_cancellationTokenSource.IsCancellationRequested)
            return;

        var task = pak.Parse(session, packet); 
        await task.WaitAsync(_maximumRunTimeInSec, _cancellationTokenSource.Token).ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                var habbo = session.GetHabboOrNull();
                foreach (var e in t.Exception.Flatten().InnerExceptions)
                {
                    _logger.LogError("Error handling packet {packetId} for session {session} @ Habbo  {username}: {message} {stacktrace}", pak.GetType().Name, session.Id, habbo?.Username ?? string.Empty, e.Message, e.StackTrace);
                    session.Disconnect($"Packet handler exception in {pak.GetType().Name}: {e.Message}");
                }
            }
        });
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _incomingPackets.Clear();
        _handshakePackets.Clear();
        _packetNames.Clear();
    }
}
