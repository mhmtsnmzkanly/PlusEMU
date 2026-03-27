using Dapper;
using Plus.Database;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Items;

public class ItemLoader : IItemLoader
{
    private readonly IDatabase _database;
    private readonly IItemDataManager _itemDataManager;

    public ItemLoader(IDatabase database, IItemDataManager itemDataManager)
    {
        _database = database;
        _itemDataManager = itemDataManager;
    }

    public List<Item> GetItemsForRoom(uint roomId, Room room)
    {
        var items = new List<Item>();
        using var db = _database.Connection();
        var rows = db.Query(
            "SELECT `items`.*, COALESCE(`items_groups`.`group_id`, 0) AS `group_id` FROM `items` LEFT OUTER JOIN `items_groups` ON `items`.`id` = `items_groups`.`id` WHERE `items`.`room_id` = @rid",
            new { rid = roomId });
        foreach (var row in rows)
        {
            var baseItemId = Convert.ToUInt32(row.base_item);
            if (_itemDataManager.Items.TryGetValue(baseItemId, out ItemDefinition? data))
            {
                items.Add(new()
                {
                    Id = Convert.ToUInt32(row.id),
                    UserId = (int)row.user_id,
                    Definition = data!,
                    ExtraData = FurniObjectData.Empty,
                    GetX = (int)row.x,
                    GetY = (int)row.y,
                    GetZ = Convert.ToDouble(row.z),
                    Rotation = (int)row.rot,
                    UniqueNumber = Convert.ToUInt32(row.limited_number),
                    UniqueSeries = Convert.ToUInt32(row.limited_stack),
                    WallCoordinates = ((string?)row.wall_pos) ?? string.Empty,
                    RoomId = roomId
                });
            }
        }
        return items;
    }

    public List<InventoryItem> GetItemsForUser(uint userId)
    {
        var items = new List<InventoryItem>();
        using var db = _database.Connection();
        var rows = db.Query(
            "SELECT `items`.*, COALESCE(`items_groups`.`group_id`, 0) AS `group_id` FROM `items` LEFT OUTER JOIN `items_groups` ON `items`.`id` = `items_groups`.`id` WHERE `items`.`room_id` = 0 AND `items`.`user_id` = @uid",
            new { uid = userId });
        foreach (var row in rows)
        {
            var baseItemId = Convert.ToUInt32(row.base_item);
            if (_itemDataManager.Items.TryGetValue(baseItemId, out ItemDefinition? data))
            {
                items.Add(new()
                {
                    Id = Convert.ToUInt32(row.id),
                    OwnerId = userId,
                    Definition = data!,
                    ExtraData = FurniObjectData.Empty,
                    UniqueNumber = Convert.ToUInt32(row.limited_number),
                    UniqueSeries = Convert.ToUInt32(row.limited_stack)
                });
            }
        }
        return items;
    }

    public void DeleteAllInventoryItemsForUser(int userId)
    {
        using var db = _database.Connection();
        db.Execute("DELETE FROM `items` WHERE `room_id` = '0' AND `user_id` = @userId", new { userId });
    }
}
