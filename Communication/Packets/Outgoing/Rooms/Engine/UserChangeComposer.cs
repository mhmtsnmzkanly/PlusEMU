using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.Communication.Packets.Outgoing.Rooms.Engine;

public class UserChangeComposer : IServerPacket
{
    private int _virtualId;
    private string _look;
    private string _gender;
    private string _motto;
    private int _achievementScore;

    public uint MessageId => ServerPacketHeader.UserChangeComposer;

    public UserChangeComposer(RoomUser user, bool self)
    {
        _virtualId = self ? -1 : user.VirtualId;
        var habbo = user.GetClient()?.GetHabbo();
        _look = habbo?.Look ?? string.Empty;
        _gender = habbo?.Gender ?? string.Empty;
        _motto = habbo?.Motto ?? string.Empty;
        _achievementScore = habbo?.HabboStats?.AchievementPoints ?? 0;
    }
    public UserChangeComposer(RoomBot user)
    {
        _virtualId = user.VirtualId;
        _look = user.Look;
        _gender = user.Gender;
        _motto = user.Motto;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_virtualId);
        packet.WriteString(_look);
        packet.WriteString(_gender);
        packet.WriteString(_motto);
        packet.WriteInteger(_achievementScore);
    }
}
