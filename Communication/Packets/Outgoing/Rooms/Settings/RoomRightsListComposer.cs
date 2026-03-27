using Plus.HabboHotel.Cache;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Outgoing.Rooms.Settings;

public class RoomRightsListComposer : IServerPacket
{
    private readonly Room _instance;
    private readonly ICacheManager _cacheManager;
    public uint MessageId => ServerPacketHeader.RoomRightsListComposer;

    public RoomRightsListComposer(Room instance, ICacheManager cacheManager)
    {
        _instance = instance;
        _cacheManager = cacheManager;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteUInteger(_instance.Id);
        packet.WriteInteger(_instance.UsersWithRights.Count);
        foreach (var id in _instance.UsersWithRights.ToList())
        {
            var data = _cacheManager.GenerateUser(id);
            if (data == null)
            {
                packet.WriteInteger(0);
                packet.WriteString("Unknown Error");
            }
            else
            {
                packet.WriteInteger(data.Id);
                packet.WriteString(data.Username);
            }
        }
    }
}