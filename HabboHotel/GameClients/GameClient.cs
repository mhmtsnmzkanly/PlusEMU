using System.Net.Sockets;
using Microsoft.IO;
using NLog;
using Plus.Communication.Encryption.Crypto.Prng;
using Plus.Communication.Flash;
using Plus.Communication.Packets;
using Plus.Communication.Revisions;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.GameClients;

public abstract class GameClient
{
    private readonly IGameServer _server;
    private readonly IPacketFactory _packetFactory;
    private static readonly ILogger Log = LogManager.GetLogger("Plus.HabboHotel.GameClients.GameClient");
    private Habbo? _habbo;
    private bool _hasDisconnected;

    public RecyclableMemoryStream? _incompleteStream;
    public Arc4? Rc4Client { get; set; }

    public bool IsAuthenticated { get; set; } = false;
    public DateTime TimeConnected { get; set; }

    public string MachineId { get; set; } = string.Empty;

    public Revision Revision { get; set; } = null!;
    public string? ClientBuild { get; set; }

    internal Func<SocketAsyncEventArgs, bool> SendCallback { get; set; } = _ => false;
    internal Action<string?>? DisconnectRequested { get; set; }

    public Guid Id { get; set; }


    public void Disconnect(string? reason = null) => DisconnectRequested?.Invoke(reason);
    public Habbo? GetHabboOrNull() => _habbo;

    protected GameClient(IGameServer server, IPacketFactory packetFactory)
    {
        _packetFactory = packetFactory;
        _server = server;
    }

    internal Habbo? OnDisconnected()
    {
        if (_hasDisconnected)
            return null;

        _hasDisconnected = true;
        var habbo = _habbo;
        if (habbo == null)
            return null;

        habbo.DetachClient();
        habbo.OnDisconnect();
        return habbo;
    }

    internal abstract (bool Complete, uint MessageId, int HeaderLength, int Length) GetMessageIdAndPacketLength(ReadOnlyMemory<byte> buffer);
    internal virtual async void OnReceived(byte[] buffer, long offset, long size)
    {
        if (size > int.MaxValue) throw new InvalidOperationException("");
        await using var stream = PlusMemoryStream.GetStream(buffer.AsSpan().Slice((int) offset, (int) size));
        var memory = stream.GetMemory().Slice(0, (int)stream.Length);

        if (_incompleteStream != null)
        {
            _incompleteStream.Write(memory.Span);
            memory = _incompleteStream.GetMemory().Slice(0, (int)_incompleteStream.Length);
        }

        while (memory.Length > 0)
        {
            var (complete, messageId, headerLength, length) = GetMessageIdAndPacketLength(memory);
            if (!complete)
            {
                _incompleteStream ??= PlusMemoryStream.GetStream(memory.Span);
                break;
            }

            try
            {
                if (Revision.IncomingIdToInternalIdMapping.TryGetValue(messageId, out var internalMessageId))
                {
                    await _server.PacketReceived(this, internalMessageId, _packetFactory.CreateIncomingPacket(memory.Slice(headerLength, length)));
                }
                else
                {
                    LogUnknownIncomingPacket(messageId, length);
                }
            }
            catch (Exception exception)
            {
                LogPacketHandlingFailure(messageId, exception);
            }
            memory = memory.Slice(headerLength + length);
            _incompleteStream?.Advance(headerLength + length);
        }

        if (memory.Length == 0)
        {
            _incompleteStream?.Dispose();
            _incompleteStream = null;
        }
    }

    public Habbo GetHabbo() => _habbo!;

    public void SetHabbo(Habbo habbo)
    {
        if (_habbo != null) throw new InvalidOperationException();
        _habbo = habbo;
    }

    public void Send(IServerPacket composer)
    {
        var outgoingMessageId = Revision.InternalIdToOutgoingIdMapping[composer.MessageId];
        var stream = PlusMemoryStream.GetStream();
        stream.Position = 0;
        var packet = _packetFactory.CreateOutgoingPacket(stream);
        composer.Compose(packet);
        var args = new SocketAsyncEventArgs();
        var memory = stream.GetBuffer().AsMemory().Slice(0, (int)stream.Length);
        CreateHeader(memory, outgoingMessageId);
        args.SetBuffer(memory);
        SendCallback(args);
        Log.Debug($"Send Packet: {composer.GetType().Name} (EmuId: {composer.MessageId}, ClientId: {outgoingMessageId})");
        stream.Dispose();
    }

    private void LogUnknownIncomingPacket(uint messageId, int length)
    {
        var habbo = _habbo;
        Log.Warn(
            "Unknown incoming packet received. SessionId={SessionId}, UserId={UserId}, Username={Username}, Revision={Revision}, Build={Build}, ClientHeader={ClientHeader}, Length={Length}",
            Id,
            habbo?.Id,
            habbo?.Username ?? "<unauthenticated>",
            Revision.Name,
            ClientBuild ?? "<unknown>",
            messageId,
            length);
    }

    private void LogPacketHandlingFailure(uint messageId, Exception exception)
    {
        var habbo = _habbo;
        Log.Error(
            exception,
            "Incoming packet handling failed. SessionId={SessionId}, UserId={UserId}, Username={Username}, Revision={Revision}, Build={Build}, ClientHeader={ClientHeader}",
            Id,
            habbo?.Id,
            habbo?.Username ?? "<unauthenticated>",
            Revision.Name,
            ClientBuild ?? "<unknown>",
            messageId);
    }

    public abstract void CreateHeader(Memory<byte> memory, uint messageId);
}
