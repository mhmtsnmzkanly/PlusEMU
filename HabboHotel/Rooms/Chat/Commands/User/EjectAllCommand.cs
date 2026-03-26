using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Dapper;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class EjectAllCommand : IChatCommand
{
    private readonly IGameClientManager _gameClientManager;
    private readonly IDatabase _database;
    public string Key => "ejectall";
    public string PermissionRequired => "command_ejectall";

    public string Parameters => "";

    public string Description => "Removes all of the items from the room.";

    public EjectAllCommand(IGameClientManager gameClientManager, IDatabase database)
    {
        _gameClientManager = gameClientManager;
        _database = database;
    }

    public void Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (habbo.Id == room.OwnerId)
        {
            //Let us check anyway.
            if (!room.CheckRights(session, true))
                return;
            foreach (var item in room.GetRoomItemHandler().GetWallAndFloor.ToList())
            {
                if (item == null || item.UserId == habbo.Id)
                    continue;
                var targetClient = _gameClientManager.GetClientByUserId(item.UserId);
                var targetHabbo = targetClient?.GetHabbo();
                var targetFurniture = targetHabbo?.Inventory?.Furniture;
                if (targetHabbo != null && targetClient != null && targetFurniture != null)
                {
                    room.GetRoomItemHandler().RemoveFurniture(targetClient, item.Id);
                    targetFurniture.AddItem(item.ToInventoryItem());
                    targetClient.Send(new FurniListUpdateComposer());
                }
                else
                {
                    room.GetRoomItemHandler().RemoveFurniture(session, item.Id);
                    using var connection = _database.Connection();
                    connection.Execute("UPDATE `items` SET `room_id` = '0' WHERE `id` = @id LIMIT 1", new { id = item.Id });
                }
            }
        }
        else
        {
            foreach (var item in room.GetRoomItemHandler().GetWallAndFloor.ToList())
            {
                if (item == null || item.UserId != habbo.Id)
                    continue;
                var targetClient = _gameClientManager.GetClientByUserId(item.UserId);
                var targetHabbo = targetClient?.GetHabbo();
                var targetFurniture = targetHabbo?.Inventory?.Furniture;
                if (targetHabbo != null && targetClient != null && targetFurniture != null)
                {
                    room.GetRoomItemHandler().RemoveFurniture(targetClient, item.Id);
                    targetFurniture.AddItem(item.ToInventoryItem());
                    targetClient.Send(new FurniListUpdateComposer());
                }
                else
                {
                    room.GetRoomItemHandler().RemoveFurniture(session, item.Id);
                    using var connection = _database.Connection();
                    connection.Execute("UPDATE `items` SET `room_id` = '0' WHERE `id` = @id LIMIT 1", new { id = item.Id });
                }
            }
        }
    }
}
