using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Chat;

namespace Plus.Communication.Packets.Incoming.Rooms.Chat;

public class CancelTypingEvent : IPacketEvent
{
    private readonly IChatService _chatService;

    public CancelTypingEvent(IChatService chatService)
    {
        _chatService = chatService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        _chatService.ApplyTypingStatus(session, false);
        return Task.CompletedTask;
    }
}
