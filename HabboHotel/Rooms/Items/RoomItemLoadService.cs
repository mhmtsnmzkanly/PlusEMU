using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public class RoomItemLoadService : IRoomItemLoadService
{
    public void ResetLoadedFurnitureState(ICollection<Item> floorItems, ICollection<Item> wallItems)
    {
        if (floorItems.Count > 0)
            floorItems.Clear();
        if (wallItems.Count > 0)
            wallItems.Clear();
    }

    public void EnsureOwnedItemUser(Room room, Item item)
    {
        if (item.UserId != 0)
            return;

        using var connection = room.GetDatabase().Connection();
        connection.Execute(
            "UPDATE `items` SET `user_id` = @userId WHERE `id` = @itemId LIMIT 1",
            new { itemId = item.Id, userId = room.OwnerId });
    }

    public bool TryRecoverInvalidFloorItem(Room room, Item item)
    {
        if (room.GetGameMap().ValidTile(item.GetX, item.GetY))
            return false;

        using (var connection = room.GetDatabase().Connection())
            connection.Execute(
                "UPDATE `items` SET `room_id` = 0 WHERE `id` = @id LIMIT 1",
                new { id = item.Id });

        var client = room.GetClientManager().GetClientByUserId(item.UserId);
        var clientHabbo = client?.GetHabbo();
        var furniture = clientHabbo?.Inventory?.Furniture;
        if (client != null && furniture != null)
        {
            furniture.AddItem(item.ToInventoryItem());
            client.Send(new FurniListUpdateComposer());
        }

        return true;
    }

    public void NormalizeWallItemPosition(Room room, Item item, string defaultWallPosition, Func<string, string?> wallPositionCheck)
    {
        if (string.IsNullOrWhiteSpace(item.WallCoordinates))
        {
            PersistDefaultWallPosition(room, item, defaultWallPosition);
            item.WallCoordinates = defaultWallPosition;
            return;
        }

        try
        {
            var wallParts = item.WallCoordinates.Split(':');
            if (wallParts.Length < 2)
                throw new FormatException("Invalid wall position");

            item.WallCoordinates = wallPositionCheck($":{wallParts[1]}") ?? defaultWallPosition;
        }
        catch
        {
            PersistDefaultWallPosition(room, item, defaultWallPosition);
            item.WallCoordinates = defaultWallPosition;
        }
    }

    private static void PersistDefaultWallPosition(Room room, Item item, string defaultWallPosition)
    {
        using var connection = room.GetDatabase().Connection();
        connection.Execute(
            "UPDATE `items` SET `wall_pos` = @wallPosition WHERE `id` = @id LIMIT 1",
            new { wallPosition = defaultWallPosition, id = item.Id });
    }
}
