using System.Data;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Furni;
using Plus.Database;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni;

internal class OpenGiftEvent : IPacketEvent
{
    private readonly IItemDataManager _itemDataManger;
    private readonly ICacheManager _cacheManager;
    private readonly IDatabase _database;

    public OpenGiftEvent(IItemDataManager itemDataManager, ICacheManager cacheManager , IDatabase database)
    {
        _itemDataManger = itemDataManager;
        _cacheManager = cacheManager;
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var furniture = habbo?.Inventory?.Furniture;
        if (habbo == null || furniture == null || !habbo.InRoom)
            return Task.CompletedTask;
        var room = habbo.CurrentRoom;
        if (room == null)
            return Task.CompletedTask;
        var presentId = packet.ReadUInt();
        var present = room.GetRoomItemHandler().GetItem(presentId);
        if (present == null)
            return Task.CompletedTask;
        if (present.UserId != habbo.Id)
            return Task.CompletedTask;
        DataRow? data;
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery("SELECT `base_id`,`extra_data` FROM `user_presents` WHERE `item_id` = @presentId LIMIT 1");
            dbClient.AddParameter("presentId", present.Id);
            data = dbClient.GetRow();
        }
        if (data == null)
        {
            RemoveBrokenPresent(session, room, furniture, present, "Oops! Appears there was a bug with this gift.\nWe'll just get rid of it for you.");
            return Task.CompletedTask;
        }
        if (!int.TryParse(present.LegacyDataString.Split(Convert.ToChar(5))[2], out var purchaserId))
        {
            RemoveBrokenPresent(session, room, furniture, present, "Oops! Appears there was a bug with this gift.\nWe'll just get rid of it for you.");
            return Task.CompletedTask;
        }
        var purchaser = _cacheManager.GenerateUser(purchaserId);
        if (purchaser == null)
        {
            RemoveBrokenPresent(session, room, furniture, present, "Oops! Appears there was a bug with this gift.\nWe'll just get rid of it for you.");
            return Task.CompletedTask;
        }
        if (!_itemDataManger.Items.TryGetValue(Convert.ToUInt32(data["base_id"]), out var baseItem))
        {
            RemoveBrokenPresent(session, room, furniture, present, "Oops, it appears that the item within the gift is no longer in the hotel!");
            return Task.CompletedTask;
        }
        present.MagicRemove = true;
        room.SendPacket(new ObjectUpdateComposer(present));
        var thread = new Thread(() => FinishOpenGift(session, baseItem, present, room, data));
        thread.Start();
        return Task.CompletedTask;
    }

    private void RemoveBrokenPresent(GameClient session, Room room, FurnitureInventoryComponent furniture, Item present, string message)
    {
        session.SendNotification(message);
        room.GetRoomItemHandler().RemoveFurniture(session, present.Id);
        using var dbClient = _database.GetQueryReactor();
        dbClient.RunQuery($"DELETE FROM `items` WHERE `id` = '{present.Id}' LIMIT 1");
        dbClient.RunQuery($"DELETE FROM `user_presents` WHERE `item_id` = '{present.Id}' LIMIT 1");
        furniture.RemoveItem(present.Id);
        session.Send(new FurniListRemoveComposer(present.Id));
    }

    private void FinishOpenGift(GameClient session, ItemDefinition baseItem, Item present, Room room, DataRow row)
    {
        try
        {
            if (baseItem == null || present == null || room == null || row == null)
                return;
            Thread.Sleep(1500);
            var itemIsInRoom = true;
            room.GetRoomItemHandler().RemoveFurniture(session, present.Id);
            using (var dbClient = _database.GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `items` SET `base_item` = @BaseItem, `extra_data` = @edata WHERE `id` = @itemId LIMIT 1");
                dbClient.AddParameter("itemId", present.Id);
                dbClient.AddParameter("BaseItem", row["base_id"]);
                dbClient.AddParameter("edata", row["extra_data"]);
                dbClient.RunQuery();
                dbClient.RunQuery($"DELETE FROM `user_presents` WHERE `item_id` = {present.Id} LIMIT 1");
            }
            present.BaseItem = Convert.ToInt32(row["base_id"]);
            // present.ResetBaseItem(); // TODO @80O: Disabled in item refactor
            present.LegacyDataString = Convert.ToString(row["extra_data"]) ?? string.Empty;
            var definition = present.Definition;
            if (definition == null)
                return;
            if (definition.Type == ItemType.Floor)
            {
                if (!room.GetRoomItemHandler().SetFloorItem(session, present, present.GetX, present.GetY, present.Rotation, true, false, true))
                {
                    using (var dbClient = _database.GetQueryReactor())
                    {
                        dbClient.SetQuery("UPDATE `items` SET `room_id` = '0' WHERE `id` = @itemId LIMIT 1");
                        dbClient.AddParameter("itemId", present.Id);
                        dbClient.RunQuery();
                    }
                    itemIsInRoom = false;
                }
            }
            else
            {
                using (var dbClient = _database.GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `items` SET `room_id` = '0' WHERE `id` = @itemId LIMIT 1");
                    dbClient.AddParameter("itemId", present.Id);
                    dbClient.RunQuery();
                }
                itemIsInRoom = false;
            }
            session.Send(new OpenGiftComposer(definition, present.LegacyDataString, present, itemIsInRoom));
            session.Send(new FurniListUpdateComposer());
        }
        catch
        {
            //ignored
        }
    }
}
