using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Game;

public class GameAchievementListComposer : IServerPacket
{
    private readonly GameClient _session;
    private readonly ICollection<Achievement> _achievements;
    private readonly int _gameId;
    public uint MessageId => ServerPacketHeader.GameAchievementListComposer;

    public GameAchievementListComposer(GameClient session, ICollection<Achievement> achievements, int gameId)
    {
        _session = session;
        _achievements = achievements;
        _gameId = gameId;
    }

    public void Compose(IOutgoingPacket packet)
    {
        var habbo = _session.GetHabbo();
        packet.WriteInteger(_gameId);
        packet.WriteInteger(_achievements.Count);
        foreach (var ach in _achievements.ToList())
        {
            var userData = habbo?.GetAchievementData(ach.GroupName);
            var targetLevel = userData != null ? userData.Level + 1 : 1;
            var targetLevelData = ach.Levels[targetLevel];
            packet.WriteInteger(ach.Id); // ach id
            packet.WriteInteger(targetLevel); // target level
            packet.WriteString(ach.GroupName + targetLevel); // badge
            packet.WriteInteger(targetLevelData.Requirement); // requirement
            packet.WriteInteger(targetLevelData.Requirement); // requirement
            packet.WriteInteger(targetLevelData.RewardPixels); // pixels
            packet.WriteInteger(0); // ach score
            packet.WriteInteger(userData?.Progress ?? 0); // Current progress
            packet.WriteBoolean(userData != null && userData.Level >= ach.Levels.Count); // Set 100% completed(??)
            packet.WriteString(ach.Category);
            packet.WriteString("basejump");
            packet.WriteInteger(0); // total levels
            packet.WriteInteger(0);
        }
        packet.WriteString("");

    }
}
