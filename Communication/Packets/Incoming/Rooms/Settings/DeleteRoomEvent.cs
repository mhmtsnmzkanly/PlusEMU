using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Settings;

internal class DeleteRoomEvent : IPacketEvent
{
    private readonly IGameClientManager _clientManager;
    private readonly IRoomManager _roomManager;
    private readonly IDatabase _database;

    public DeleteRoomEvent(IGameClientManager clientManager, IRoomManager roomManager, IDatabase database)
    {
        _clientManager = clientManager;
        _roomManager = roomManager;
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo)
            return Task.CompletedTask;

        var permissions = habbo.Permissions;
        var roomId = packet.ReadUInt();
        if (roomId == 0)
            return Task.CompletedTask;
        if (!_roomManager.TryGetRoom(roomId, out var room))
            return Task.CompletedTask;
        if (room.OwnerId != habbo.Id && !(permissions?.HasRight("room_delete_any") ?? false))
            return Task.CompletedTask;

        var itemsToRemove = new List<Item>();
        foreach (var item in room.GetRoomItemHandler().GetWallAndFloor.ToList())
        {
            if (item == null)
                continue;
            if (item.Definition.InteractionType == InteractionType.Moodlight)
            {
                using var db = _database.Connection();
                db.Execute("DELETE FROM `room_items_moodlight` WHERE `item_id` = @itemId LIMIT 1", new { itemId = item.Id });
            }
            itemsToRemove.Add(item);
        }
        foreach (var item in itemsToRemove)
        {
            var targetClient = _clientManager.GetClientByUserId(item.UserId);
            if (targetClient != null && targetClient.GetHabbo() != null)
            {
                room.GetRoomItemHandler().RemoveFurniture(targetClient, item.Id);
                targetClient.GetHabbo().Inventory?.Furniture.AddItem(item.ToInventoryItem());
                targetClient.Send(new FurniListUpdateComposer());
            }
            else
            {
                room.GetRoomItemHandler().RemoveFurniture(null, item.Id);
                using var db = _database.Connection();
                db.Execute("UPDATE `items` SET `room_id` = '0' WHERE `id` = @itemId LIMIT 1", new { itemId = item.Id });
            }
        }
        _roomManager.UnloadRoom(room.Id);
        using var dbFinal = _database.Connection();
        dbFinal.Execute("DELETE FROM `user_roomvisits` WHERE `room_id` = @rid", new { rid = roomId });
        dbFinal.Execute("DELETE FROM `rooms` WHERE `id` = @rid LIMIT 1", new { rid = roomId });
        dbFinal.Execute("DELETE FROM `user_favorites` WHERE `room_id` = @rid", new { rid = roomId });
        dbFinal.Execute("DELETE FROM `items` WHERE `room_id` = @rid", new { rid = roomId });
        dbFinal.Execute("DELETE FROM `room_rights` WHERE `room_id` = @rid", new { rid = roomId });
        dbFinal.Execute("UPDATE `users` SET `home_room` = '0' WHERE `home_room` = @rid", new { rid = roomId });
        return Task.CompletedTask;
    }
}
