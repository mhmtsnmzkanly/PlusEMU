using Dapper;
using Plus.Database;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items;

public class ItemTeleporterFinder : IItemTeleporterFinder
{
    private readonly IDatabase _database;

    public ItemTeleporterFinder(IDatabase database)
    {
        _database = database;
    }

    public uint GetLinkedTele(uint teleId)
    {
        using var db = _database.Connection();
        var result = db.QueryFirstOrDefault<uint?>(
            "SELECT `tele_two_id` FROM `room_items_tele_links` WHERE `tele_one_id` = @teleId LIMIT 1",
            new { teleId });
        return result ?? 0;
    }

    public uint GetTeleRoomId(uint teleId, Room room)
    {
        if (room.GetRoomItemHandler().GetItem(teleId) != null)
            return room.RoomId;
        using var db = _database.Connection();
        var result = db.QueryFirstOrDefault<uint?>(
            "SELECT `room_id` FROM `items` WHERE `id` = @teleId LIMIT 1",
            new { teleId });
        return result ?? 0;
    }

    public bool IsTeleLinked(uint teleId, Room room)
    {
        var linkId = GetLinkedTele(teleId);
        if (linkId == 0) return false;
        var item = room.GetRoomItemHandler().GetItem(linkId);
        if (item != null && item.Definition.IsTeleport)
            return true;
        var roomId = GetTeleRoomId(linkId, room);
        if (roomId == 0) return false;
        return true;
    }
}
