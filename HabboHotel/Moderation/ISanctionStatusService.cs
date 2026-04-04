using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Moderation;

public interface ISanctionStatusService
{
    Task<SanctionStatusData> GetStatus(GameClient session);
}
