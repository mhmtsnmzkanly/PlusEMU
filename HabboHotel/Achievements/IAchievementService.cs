using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Achievements;

public interface IAchievementService
{
    Task<bool> ProgressAchievement(GameClient session, string group, int progress, bool fromBeginning = false);
}
