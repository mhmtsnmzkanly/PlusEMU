using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Chat;

namespace Plus.Communication.Packets.Incoming.Rooms.Chat;

public class ShoutEvent : IPacketEvent
{
    private readonly IChatService _chatService;

    public ShoutEvent(IChatService chatService)
    {
        _chatService = chatService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var message = packet.ReadString();
        var colour = packet.ReadInt();
        return _chatService.Shout(session, message, colour);
    }
}
