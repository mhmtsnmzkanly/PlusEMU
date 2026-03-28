using Dapper;
using Plus.Database;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public class RoomItemPlacementPersistenceService : IRoomItemPlacementPersistenceService
{
    private readonly IDatabase _database;

    public RoomItemPlacementPersistenceService(IDatabase database)
    {
        _database = database;
    }

    public void SaveFloorPlacement(uint roomId, Item item)
    {
        using var connection = _database.Connection();
        connection.Execute(
            "UPDATE `items` SET `room_id` = @roomId, `x` = @x, `y` = @y, `z` = @z, `rot` = @rot WHERE `id` = @id LIMIT 1",
            new { roomId, x = item.GetX, y = item.GetY, z = item.GetZ, rot = item.Rotation, id = item.Id });
    }

    public void SaveWallPlacement(uint roomId, Item item)
    {
        using var connection = _database.Connection();
        connection.Execute(
            "UPDATE `items` SET `room_id` = @roomId, `x` = @x, `y` = @y, `z` = @z, `rot` = @rot, `wall_pos` = @wallPos WHERE `id` = @id LIMIT 1",
            new
            {
                roomId,
                x = item.GetX,
                y = item.GetY,
                z = item.GetZ,
                rot = item.Rotation,
                wallPos = item.WallCoordinates,
                id = item.Id
            });
    }
}
