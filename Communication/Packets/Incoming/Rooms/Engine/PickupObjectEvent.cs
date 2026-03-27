using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Quests;

namespace Plus.Communication.Packets.Incoming.Rooms.Engine;

internal class PickupObjectEvent : IPacketEvent
{
    private readonly IGameClientManager _clientManager;
    private readonly IQuestService _questService;
    private readonly IDatabase _database;

    public PickupObjectEvent(IGameClientManager clientManager, IQuestService questService, IDatabase database)
    {
        _clientManager = clientManager;
        _questService = questService;
        _database = database;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var furniture = habbo?.Inventory?.Furniture;
        if (habbo?.Permissions == null || furniture == null || !habbo.InRoom)
            return;
        var room = habbo.CurrentRoom;
        if (room == null)
            return;
        packet.ReadInt(); //unknown
        var itemId = packet.ReadUInt();
        var item = room.GetRoomItemHandler().GetItem(itemId);
        if (item == null)
            return;
        if (item.Definition.InteractionType == InteractionType.Postit)
            return;
        var itemRights = false;
        if (item.UserId == habbo.Id || room.CheckRights(session, false))
            itemRights = true;
        else if (room.Group != null && room.CheckRights(session, false, true)) //Room has a group, this user has group rights.
            itemRights = true;
        else if (habbo.Permissions.HasRight("room_item_take"))
            itemRights = true;
        if (itemRights)
        {
            using var connection = _database.Connection();
            if (item.Definition.InteractionType == InteractionType.Tent || item.Definition.InteractionType == InteractionType.TentSmall)
                room.RemoveTent(item.Id);
            if (item.Definition.InteractionType == InteractionType.Moodlight)
            {
                await connection.ExecuteAsync("DELETE FROM `room_items_moodlight` WHERE `item_id` = @id LIMIT 1", new { id = item.Id });
            }
            else if (item.Definition.InteractionType == InteractionType.Toner)
            {
                await connection.ExecuteAsync("DELETE FROM `room_items_toner` WHERE `id` = @id LIMIT 1", new { id = item.Id });
            }
            if (item.UserId == habbo.Id || habbo.Permissions.HasRight("room_item_take"))
            {
                room.GetRoomItemHandler().RemoveFurniture(session, item.Id);
                furniture.AddItem(item.ToInventoryItem());
                session.Send(new FurniListUpdateComposer());
            }
            else //Item is being ejected.
            {
                var targetClient = _clientManager.GetClientByUserId(item.UserId);
                var targetFurniture = targetClient?.GetHabbo()?.Inventory?.Furniture;
                if (targetClient != null && targetFurniture != null) //Again, do we have an active client?
                {
                    room.GetRoomItemHandler().RemoveFurniture(targetClient, item.Id);
                    targetFurniture.AddItem(item.ToInventoryItem());
                    targetClient.Send(new FurniListUpdateComposer());
                }
                else //No, query time.
                {
                    room.GetRoomItemHandler().RemoveFurniture(null, item.Id);
                }
            }

            await connection.ExecuteAsync("UPDATE `items` SET `room_id` = '0' WHERE `id` = @id LIMIT 1", new { id = item.Id });

            await _questService.ProgressUserQuest(session, QuestType.FurniPick);
        }
    }
}
