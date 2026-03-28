using Dapper;
using Plus.Database;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public class RoomItemPersistenceService : IRoomItemPersistenceService
{
    private readonly IDatabase _database;

    public RoomItemPersistenceService(IDatabase database)
    {
        _database = database;
    }

    public void SaveMovedItems(IEnumerable<Item> items)
    {
        using var connection = _database.Connection();
        foreach (var item in items)
            PersistMovedItem(connection, item);
    }

    private static void PersistMovedItem(System.Data.IDbConnection connection, Item item)
    {
        PersistMovedItemExtraData(connection, item);
        PersistMovedWallItemPosition(connection, item);
        PersistMovedItemCoordinates(connection, item);
    }

    private static void PersistMovedItemExtraData(System.Data.IDbConnection connection, Item item)
    {
        if (string.IsNullOrEmpty(item.LegacyDataString))
            return;

        connection.Execute(
            "UPDATE `items` SET `extra_data` = @extraData WHERE `id` = @id LIMIT 1",
            new { extraData = item.ExtraData.Serialize(), id = item.Id });
    }

    private static void PersistMovedWallItemPosition(System.Data.IDbConnection connection, Item item)
    {
        if (!item.IsWallItem || IsRoomSurfaceDecoration(item))
            return;

        connection.Execute(
            "UPDATE `items` SET `wall_pos` = @wallPos WHERE `id` = @id LIMIT 1",
            new { wallPos = item.WallCoordinates, id = item.Id });
    }

    private static bool IsRoomSurfaceDecoration(Item item) =>
        item.Definition.ItemName.Contains("wallpaper_single") ||
        item.Definition.ItemName.Contains("floor_single") ||
        item.Definition.ItemName.Contains("landscape_single");

    private static void PersistMovedItemCoordinates(System.Data.IDbConnection connection, Item item)
    {
        connection.Execute(
            "UPDATE `items` SET `x` = @x, `y` = @y, `z` = @z, `rot` = @rot WHERE `id` = @id LIMIT 1",
            new { x = item.GetX, y = item.GetY, z = item.GetZ, rot = item.Rotation, id = item.Id });
    }
}
