using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Chat;

namespace Plus.Communication.Packets.Incoming.Rooms.Chat;

public class WhisperEvent : IPacketEvent
{
    private readonly IChatService _chatService;

    public WhisperEvent(IChatService chatService)
    {
        _chatService = chatService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var @params = packet.ReadString();
        var colour = packet.ReadInt();
        if (string.IsNullOrWhiteSpace(@params) || !@params.Contains(' '))
            return Task.CompletedTask;

        var toUser = @params.Split(' ')[0];
        var message = @params.Substring(toUser.Length + 1);
        return _chatService.Whisper(session, toUser, message, colour);
    }
}
