using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Moderation;

public interface IModerationTicketService
{
    Task SendOpenState(GameClient session);
    Task Submit(GameClient session, string message, int category, int reportedUserId, int type, IReadOnlyCollection<ModerationTicketChatEntry> reportedChats);
    Task Close(GameClient session, int result, int ticketId);
    Task Pick(GameClient session, int ticketId);
    Task Release(GameClient session, IReadOnlyCollection<int> ticketIds);
    Task DeletePendingCalls(GameClient session);
}
