using Dapper;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items;

public static class ItemTeleporterFinder
{
    public static uint GetLinkedTele(uint teleId)
    {
        using var db = PlusEnvironment.DatabaseManager.Connection();
        var result = db.QueryFirstOrDefault<uint?>(
            "SELECT `tele_two_id` FROM `room_items_tele_links` WHERE `tele_one_id` = @teleId LIMIT 1",
            new { teleId });
        return result ?? 0;
    }

    public static uint GetTeleRoomId(uint teleId, Room pRoom)
    {
        if (pRoom.GetRoomItemHandler().GetItem(teleId) != null)
            return pRoom.RoomId;
        using var db = PlusEnvironment.DatabaseManager.Connection();
        var result = db.QueryFirstOrDefault<uint?>(
            "SELECT `room_id` FROM `items` WHERE `id` = @teleId LIMIT 1",
            new { teleId });
        return result ?? 0;
    }

    public static bool IsTeleLinked(uint teleId, Room pRoom)
    {
        var linkId = GetLinkedTele(teleId);
        if (linkId == 0) return false;
        var item = pRoom.GetRoomItemHandler().GetItem(linkId);
        if (item != null && item.Definition.InteractionType == InteractionType.Teleport)
            return true;
        var roomId = GetTeleRoomId(linkId, pRoom);
        if (roomId == 0) return false;
        return true;
    }
}