using Plus.Core.Settings;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Outgoing.Groups;

public class GroupCreationWindowComposer : IServerPacket
{
    private readonly ICollection<RoomData> _rooms;
    private readonly ISettingsManager _settingsManager;
    public uint MessageId => ServerPacketHeader.GroupCreationWindowComposer;

    public GroupCreationWindowComposer(ICollection<RoomData> rooms, ISettingsManager settingsManager)
    {
        _rooms = rooms;
        _settingsManager = settingsManager;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_settingsManager.GetIntOrDefault("catalog.group.purchase.cost", 0)); //Price 
        packet.WriteInteger(_rooms.Count); //Room count that the user has.
        foreach (var room in _rooms)
        {
            packet.WriteUInteger(room.Id); //Room Id
            packet.WriteString(room.Name); //Room Name
            packet.WriteBoolean(false); //What?
        }
        packet.WriteInteger(5);
        packet.WriteInteger(5);
        packet.WriteInteger(11);
        packet.WriteInteger(4);
        packet.WriteInteger(6);
        packet.WriteInteger(11);
        packet.WriteInteger(4);
        packet.WriteInteger(0);
        packet.WriteInteger(0);
        packet.WriteInteger(0);
        packet.WriteInteger(0);
        packet.WriteInteger(0);
        packet.WriteInteger(0);
        packet.WriteInteger(0);
        packet.WriteInteger(0);
        packet.WriteInteger(0);
    }
}
