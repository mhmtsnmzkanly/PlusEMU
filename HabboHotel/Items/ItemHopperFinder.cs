using Dapper;
using Plus.Database;

namespace Plus.HabboHotel.Items;

public class ItemHopperFinder : IItemHopperFinder
{
    private readonly IDatabase _database;

    public ItemHopperFinder(IDatabase database)
    {
        _database = database;
    }

    public uint GetAHopper(uint curRoom)
    {
        using var db = _database.Connection();
        var result = db.QueryFirstOrDefault<uint?>(
            "SELECT `room_id` FROM `items_hopper` WHERE `room_id` <> @room ORDER BY `room_id` ASC LIMIT 1",
            new { room = curRoom });
        return result ?? 0;
    }

    public uint GetHopperId(uint nextRoom)
    {
        using var db = _database.Connection();
        var result = db.QueryFirstOrDefault<uint?>(
            "SELECT `hopper_id` FROM `items_hopper` WHERE `room_id` = @room LIMIT 1",
            new { room = nextRoom });
        return result ?? 0;
    }
}