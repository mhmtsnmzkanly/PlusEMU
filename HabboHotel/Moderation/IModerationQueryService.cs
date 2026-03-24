using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Moderation;

public interface IModerationQueryService
{
    Task GetUserInfo(GameClient session, int userId);
    Task GetRoomInfo(GameClient session, uint roomId);
    Task GetUserRoomVisits(GameClient session, int userId);
    Task GetUserChatlog(GameClient session, int userId);
    Task GetRoomChatlog(GameClient session, uint roomId);
    Task GetTicketChatlogs(GameClient session, int ticketId);
}
