using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items;

public interface IItemTeleporterFinder
{
    uint GetLinkedTele(uint teleId);
    uint GetTeleRoomId(uint teleId, Room room);
    bool IsTeleLinked(uint teleId, Room room);
}
