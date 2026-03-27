using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.Utilities;

namespace Plus.Communication.Packets.Outgoing.Moderation;

public class ModeratorUserRoomVisitsComposer : IServerPacket
{
    private readonly int _userId;
    private readonly string _username;
    private readonly Dictionary<double, RoomData> _visits;
    public uint MessageId => ServerPacketHeader.ModeratorUserRoomVisitsComposer;

    public ModeratorUserRoomVisitsComposer(int userId, string username, Dictionary<double, RoomData> visits)
    {
        _userId = userId;
        _username = username;
        _visits = visits;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_userId);
        packet.WriteString(_username);
        packet.WriteInteger(_visits.Count);
        foreach (var (key, roomData) in _visits)
        {
            packet.WriteUInteger(roomData.Id);
            packet.WriteString(roomData.Name);
            packet.WriteInteger(UnixTimestamp.FromUnixTimestamp(key).Hour);
            packet.WriteInteger(UnixTimestamp.FromUnixTimestamp(key).Minute);
        }
    }
}