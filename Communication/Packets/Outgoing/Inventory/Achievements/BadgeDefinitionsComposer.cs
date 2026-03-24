using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Inventory.Achievements;

public class BadgeDefinitionsComposer : IServerPacket
{
    private readonly Dictionary<string, Achievement> _achievements;
    public uint MessageId => ServerPacketHeader.BadgeDefinitionsComposer;

    public BadgeDefinitionsComposer(Dictionary<string, Achievement> achievements)
    {
        _achievements = achievements;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_achievements.Count);
        foreach (var achievement in _achievements.Values)
        {
            packet.WriteString((achievement.GroupName ?? string.Empty).Replace("ACH_", ""));
            packet.WriteInteger(achievement.Levels.Count);
            foreach (var level in achievement.Levels.Values)
            {
                packet.WriteInteger(level.Level);
                packet.WriteInteger(level.Requirement);
            }
        }
    }
}
