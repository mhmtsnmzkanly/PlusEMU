using Dapper;
using Plus.Database;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items;

public class ItemFactory : IItemFactory
{
    private readonly IDatabase _database;
    public static ItemFactory? Instance { get; set; }

    public ItemFactory(IDatabase database)
    {
        _database = database;
    }

    public Item CreateSingleItemNullable(ItemDefinition definition, Habbo habbo, string extraData, string displayFlags, int groupId = 0, uint limitedNumber = 0, uint limitedStack = 0)
    {
        if (definition == null) throw new InvalidOperationException("Data cannot be null.");
        var item = new Item()
        {
            OwnerId = (uint)habbo.Id,
            Definition = definition,
            ExtraData = new LegacyDataFormat() { Data = extraData },
            UniqueNumber = limitedNumber,
            UniqueSeries = limitedStack,
            GroupId = groupId
        };
        using var db = PlusEnvironment.DatabaseManager.Connection();
        item.Id = Convert.ToUInt32(db.ExecuteScalar<long>(
            "INSERT INTO `items` (`base_item`,`user_id`,`room_id`,`x`,`y`,`z`,`wall_pos`,`rot`,`extra_data`,`limited_number`,`limited_stack`) VALUES (@did,@uid,0,0,0,0,'',0,@extra,@lnum,@lstack); SELECT LAST_INSERT_ID();",
            new { did = definition.Id, uid = habbo.Id, extra = extraData, lnum = limitedNumber, lstack = limitedStack }));
        if (groupId > 0)
            db.Execute("INSERT INTO `items_groups` (`id`, `group_id`) VALUES (@id, @gid)", new { id = item.Id, gid = groupId });
        return item;
    }

    public Item CreateSingleItem(ItemDefinition definition, Habbo habbo, string extraData, string displayFlags, uint itemId, uint limitedNumber = 0, uint limitedStack = 0)
    {
        if (definition == null) throw new InvalidOperationException("Data cannot be null.");
        var item = new Item()
        {
            Id = itemId,
            OwnerId = (uint)habbo.Id,
            Definition = definition,
            ExtraData = new LegacyDataFormat() { Data = extraData },
            UniqueNumber = limitedNumber,
            UniqueSeries = limitedStack
        };
        using var db = PlusEnvironment.DatabaseManager.Connection();
        db.Execute(
            "INSERT INTO `items` (`id`,`base_item`,`user_id`,`room_id`,`x`,`y`,`z`,`wall_pos`,`rot`,`extra_data`,`limited_number`,`limited_stack`) VALUES (@id,@did,@uid,0,0,0,0,'',0,@extra,@lnum,@lstack)",
            new { id = itemId, did = definition.Id, uid = habbo.Id, extra = extraData, lnum = limitedNumber, lstack = limitedStack });
        return item;
    }

    public Item CreateGiftItem(ItemDefinition definition, Habbo habbo, string extraData, string displayFlags, int itemId, uint limitedNumber = 0, uint limitedStack = 0)
    {
        if (definition == null) throw new InvalidOperationException("Data cannot be null.");
        var item = new Item()
        {
            OwnerId = (uint)habbo.Id,
            Definition = definition,
            ExtraData = new LegacyDataFormat() { Data = extraData },
            UniqueNumber = limitedNumber,
            UniqueSeries = limitedStack,
        };
        using var db = PlusEnvironment.DatabaseManager.Connection();
        db.Execute(
            "INSERT INTO `items` (`id`,`base_item`,`user_id`,`room_id`,`x`,`y`,`z`,`wall_pos`,`rot`,`extra_data`,`limited_number`,`limited_stack`) VALUES (@id,@did,@uid,0,0,0,0,'',0,@extra,@lnum,@lstack)",
            new { id = itemId, did = definition.Id, uid = habbo.Id, extra = extraData, lnum = limitedNumber, lstack = limitedStack });
        return item;
    }

    public List<Item> CreateMultipleItems(ItemDefinition definition, Habbo habbo, string extraData, int amount, int groupId = 0)
    {
        if (definition == null) throw new InvalidOperationException("Data cannot be null.");
        var items = new List<Item>();
        using var db = PlusEnvironment.DatabaseManager.Connection();
        for (var i = 0; i < amount; i++)
        {
            var newId = Convert.ToUInt32(db.ExecuteScalar<long>(
                "INSERT INTO `items` (`base_item`,`user_id`,`room_id`,`x`,`y`,`z`,`wall_pos`,`rot`,`extra_data`) VALUES (@did,@uid,0,0,0,0,'',0,@flags); SELECT LAST_INSERT_ID();",
                new { did = definition.Id, uid = habbo.Id, flags = extraData }));
            var item = new Item()
            {
                Id = newId,
                OwnerId = (uint)habbo.Id,
                Definition = definition,
                ExtraData = new LegacyDataFormat() { Data = extraData },
                GroupId = groupId
            };
            if (groupId > 0)
                db.Execute("INSERT INTO `items_groups` (`id`, `group_id`) VALUES (@id, @gid)", new { id = item.Id, gid = groupId });
            items.Add(item);
        }
        return items;
    }

    public List<Item> CreateTeleporterItems(ItemDefinition definition, Habbo habbo, int groupId = 0)
    {
        using var db = PlusEnvironment.DatabaseManager.Connection();
        var item1Id = Convert.ToUInt32(db.ExecuteScalar<long>(
            "INSERT INTO `items` (`base_item`,`user_id`,`room_id`,`x`,`y`,`z`,`wall_pos`,`rot`,`extra_data`) VALUES (@did,@uid,0,0,0,0,'',0,''); SELECT LAST_INSERT_ID();",
            new { did = definition.Id, uid = habbo.Id }));
        var item2Id = Convert.ToUInt32(db.ExecuteScalar<long>(
            "INSERT INTO `items` (`base_item`,`user_id`,`room_id`,`x`,`y`,`z`,`wall_pos`,`rot`,`extra_data`) VALUES (@did,@uid,0,0,0,0,'',0,@flags); SELECT LAST_INSERT_ID();",
            new { did = definition.Id, uid = habbo.Id, flags = item1Id.ToString() }));
        db.Execute(
            "INSERT INTO `room_items_tele_links` (`tele_one_id`, `tele_two_id`) VALUES (@a, @b), (@b, @a)",
            new { a = item1Id, b = item2Id });
        var item1 = new Item() { Id = item1Id, OwnerId = (uint)habbo.Id, Definition = definition, ExtraData = new LegacyDataFormat(), GroupId = groupId };
        var item2 = new Item() { Id = item2Id, OwnerId = (uint)habbo.Id, Definition = definition, ExtraData = new LegacyDataFormat(), GroupId = groupId };
        return [item1, item2];
    }

    public void CreateMoodlightData(Item item)
    {
        using var db = PlusEnvironment.DatabaseManager.Connection();
        db.Execute(
            "INSERT INTO `room_items_moodlight` (`id`, `enabled`, `current_preset`, `preset_one`, `preset_two`, `preset_three`) VALUES (@id, '0', 1, @preset, @preset, @preset)",
            new { id = item.Id, preset = "#000000,255,0" });
    }

    public void CreateTonerData(Item item)
    {
        using var db = PlusEnvironment.DatabaseManager.Connection();
        db.Execute(
            "INSERT INTO `room_items_toner` (`id`, `data1`, `data2`, `data3`, `enabled`) VALUES (@id, 0, 0, 0, '0')",
            new { id = item.Id });
    }
}
