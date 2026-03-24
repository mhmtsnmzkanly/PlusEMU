using Plus.Communication.Packets.Outgoing.Inventory.AvatarEffects;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.Database;
using Plus.HabboHotel.Catalog.Clothing;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni;

internal class UseSellableClothingEvent : IPacketEvent
{
    private readonly IClothingManager _clothingManager;
    private readonly IDatabase _database;

    public UseSellableClothingEvent(IClothingManager clothingManager, IDatabase database)
    {
        _clothingManager = clothingManager;
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var clothingComponent = habbo?.Clothing;
        if (habbo == null || clothingComponent == null || !habbo.InRoom)
            return Task.CompletedTask;
        var room = habbo.CurrentRoom;
        if (room == null)
            return Task.CompletedTask;
        var itemId = packet.ReadUInt();
        var item = room.GetRoomItemHandler().GetItem(itemId);
        if (item == null)
            return Task.CompletedTask;
        if (item.Definition == null)
            return Task.CompletedTask;
        if (item.UserId != habbo.Id)
            return Task.CompletedTask;
        if (item.Definition.InteractionType != InteractionType.PurchasableClothing)
        {
            session.SendNotification("Oops, this item isn't set as a sellable clothing item!");
            return Task.CompletedTask;
        }
        if (item.Definition.BehaviourData == 0)
        {
            session.SendNotification("Oops, this item doesn't have a linking clothing configuration, please report it!");
            return Task.CompletedTask;
        }
        if (!_clothingManager.TryGetClothing(item.Definition.BehaviourData, out var clothing))
        {
            session.SendNotification("Oops, we couldn't find this clothing part!");
            return Task.CompletedTask;
        }

        //Quickly delete it from the database.
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery("DELETE FROM `items` WHERE `id` = @ItemId LIMIT 1");
            dbClient.AddParameter("ItemId", item.Id);
            dbClient.RunQuery();
        }

        //Remove the item.
        room.GetRoomItemHandler().RemoveFurniture(session, item.Id);
        clothingComponent.AddClothing(clothing.ClothingName, clothing.PartIds);
        session.Send(new FigureSetIdsComposer(clothingComponent.GetClothingParts));
        session.Send(new RoomNotificationComposer("figureset.redeemed.success"));
        session.SendWhisper("If for some reason cannot see your new clothing, reload the hotel!");
        return Task.CompletedTask;
    }
}
