using Dapper;

namespace Plus.HabboHotel.Items;

/// TODO @80O: Make this an injectable service. Pass database via constructor. Use Dapper
public static class ItemHopperFinder
{
    public static uint GetAHopper(uint curRoom)
    {
        using var db = PlusEnvironment.DatabaseManager.Connection();
        var result = db.QueryFirstOrDefault<uint?>(
            "SELECT `room_id` FROM `items_hopper` WHERE `room_id` <> @room ORDER BY `room_id` ASC LIMIT 1",
            new { room = curRoom });
        return result ?? 0;
    }

    public static uint GetHopperId(uint nextRoom)
    {
        using var db = PlusEnvironment.DatabaseManager.Connection();
        var result = db.QueryFirstOrDefault<uint?>(
            "SELECT `hopper_id` FROM `items_hopper` WHERE `room_id` = @room LIMIT 1",
            new { room = nextRoom });
        return result ?? 0;
    }
}