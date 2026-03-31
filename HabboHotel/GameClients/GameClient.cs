using System.Net.Sockets;
using System.Text;
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

        habbo.OnDisconnect();
        habbo.DetachClient();
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
                    LogUnknownIncomingPacket(messageId, memory.Slice(headerLength, length));
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
        if (_hasDisconnected)
        {
            Log.Debug("Dropped Packet: {PacketName} because session {SessionId} is disconnected.", composer.GetType().Name, Id);
            return;
        }

        var outgoingMessageId = Revision.InternalIdToOutgoingIdMapping[composer.MessageId];
        var stream = PlusMemoryStream.GetStream();
        stream.Position = 0;
        var packet = _packetFactory.CreateOutgoingPacket(stream);
        composer.Compose(packet);
        var args = new SocketAsyncEventArgs();
        var memory = stream.GetBuffer().AsMemory().Slice(0, (int)stream.Length);
        CreateHeader(memory, outgoingMessageId);
        args.SetBuffer(memory);
        var sent = SendCallback(args);
        if (sent)
            Log.Debug($"Send Packet: {composer.GetType().Name} (EmuId: {composer.MessageId}, ClientId: {outgoingMessageId})");
        else
            Log.Debug("Dropped Packet: {PacketName} (EmuId: {EmuId}, ClientId: {ClientId}) for session {SessionId} because transport rejected the send.", composer.GetType().Name, composer.MessageId, outgoingMessageId, Id);
        stream.Dispose();
    }

    private void LogUnknownIncomingPacket(uint messageId, ReadOnlyMemory<byte> payload)
    {
        var habbo = _habbo;
        var revisionPacketName = Revision.IncomingHeaders.FirstOrDefault(header => header.Value == messageId).Key;
        var previewLength = Math.Min(payload.Length, 32);
        var previewBytes = payload.Span[..previewLength].ToArray();
        var hexPreview = Convert.ToHexString(previewBytes);
        var utf8Preview = SanitizePayloadPreview(Encoding.UTF8.GetString(previewBytes));
        Log.Warn(
            "Unknown incoming packet received. SessionId={SessionId}, UserId={UserId}, Username={Username}, Revision={Revision}, Build={Build}, ClientHeader={ClientHeader}, RevisionPacket={RevisionPacket}, Length={Length}, HexPreview={HexPreview}, Utf8Preview={Utf8Preview}",
            Id,
            habbo?.Id,
            habbo?.Username ?? "<unauthenticated>",
            Revision.Name,
            ClientBuild ?? "<unknown>",
            messageId,
            string.IsNullOrWhiteSpace(revisionPacketName) ? "<unmapped-in-revision>" : revisionPacketName,
            payload.Length,
            hexPreview,
            utf8Preview);
    }

    private static string SanitizePayloadPreview(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
            builder.Append(char.IsControl(character) ? '.' : character);
        return builder.ToString();
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
