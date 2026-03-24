using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Moderation;

public interface IModerationActionService
{
    Task SendCaution(GameClient session, int userId, string message);
    Task SendMessage(GameClient session, int userId, string message);
    Task Mute(GameClient session, int userId, int durationMinutes);
    Task Kick(GameClient session, int userId);
    Task Ban(GameClient session, int userId, string message, int durationHours, bool ipBan, bool machineBan);
    Task TradeLock(GameClient session, int userId, string message, int durationMinutes);
    Task BroadcastRoomAction(GameClient session, int alertMode, string alertMessage);
}
