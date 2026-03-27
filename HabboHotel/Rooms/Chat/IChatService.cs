using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat;

public interface IChatService
{
    Task Chat(GameClient session, string message, int styleId);
    Task Shout(GameClient session, string message, int styleId);
    Task Whisper(GameClient session, string targetUser, string message, int styleId);
    void ApplyTypingStatus(GameClient session, bool isTyping);
}
