using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Inventory.Achievements;

public class AchievementsComposer : IServerPacket
{
    private readonly GameClient _session;
    private readonly List<Achievement> _achievements;
    public uint MessageId => ServerPacketHeader.AchievementsComposer;

    public AchievementsComposer(GameClient session, List<Achievement> achievements)
    {
        _session = session;
        _achievements = achievements;
    }

    public void Compose(IOutgoingPacket packet)
    {
        var habbo = _session.GetHabbo();
        packet.WriteInteger(_achievements.Count);
        foreach (var achievement in _achievements)
        {
            var groupName = achievement.GroupName ?? string.Empty;
            var category = achievement.Category ?? string.Empty;
            var userData = habbo?.GetAchievementData(groupName);
            var targetLevel = userData != null ? userData.Level + 1 : 1;
            var totalLevels = achievement.Levels.Count;
            targetLevel = targetLevel > totalLevels ? totalLevels : targetLevel;
            var targetLevelData = achievement.Levels[targetLevel];
            packet.WriteInteger(achievement.Id); // Unknown (ID?)
            packet.WriteInteger(targetLevel); // Target level
            packet.WriteString(groupName + targetLevel); // Target name/desc/badge
            packet.WriteInteger(1);
            packet.WriteInteger(targetLevelData.Requirement); // Progress req/target
            packet.WriteInteger(targetLevelData.RewardPixels);
            packet.WriteInteger(0); // Type of reward
            packet.WriteInteger(userData?.Progress ?? 0); // Current progress
            packet.WriteBoolean(userData != null ? userData.Level >= totalLevels : false); // Set 100% completed(??)
            packet.WriteString(category); // Category
            packet.WriteString(string.Empty);
            packet.WriteInteger(totalLevels); // Total amount of levels
            packet.WriteInteger(0);
        }
        packet.WriteString("");

    }
}
