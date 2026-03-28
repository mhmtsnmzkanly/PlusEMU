using Dapper;
using Plus.Communication.Packets.Outgoing.Avatar;
using Plus.Communication.Packets.Outgoing.Inventory.AvatarEffects;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.Core.FigureData;
using Plus.Database;
using Plus.HabboHotel.Catalog.Clothing;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Users.Clothing;

internal class AvatarClothingService : IAvatarClothingService
{
    private readonly IWardrobeLoader _wardrobeLoader;
    private readonly IFigureDataManager _figureDataManager;
    private readonly IDatabase _database;
    private readonly IClothingManager _clothingManager;

    public AvatarClothingService(
        IWardrobeLoader wardrobeLoader,
        IFigureDataManager figureDataManager,
        IDatabase database,
        IClothingManager clothingManager)
    {
        _wardrobeLoader = wardrobeLoader;
        _figureDataManager = figureDataManager;
        _database = database;
        _clothingManager = clothingManager;
    }

    public async Task GetWardrobe(GameClient session)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        var wardrobe = await _wardrobeLoader.LoadUserWardrobe(habbo.Id);
        if (wardrobe != null)
            session.Send(new WardrobeComposer(wardrobe));
    }

    public Task SaveWardrobeOutfit(GameClient session, int slotId, string look, string gender)
    {
        var habbo = session.GetHabbo();
        var clothing = habbo?.Clothing;
        if (habbo == null || clothing == null)
            return Task.CompletedTask;

        var processedLook = _figureDataManager.ProcessFigure(look, gender, clothing.GetClothingParts, true);

        using var connection = _database.Connection();
        var rows = connection.Execute(
            "SELECT null FROM `user_wardrobe` WHERE `user_id` = @id AND `slot_id` = @slot",
            new { id = habbo.Id, slot = slotId });

        if (rows == 1)
        {
            connection.Execute(
                "UPDATE `user_wardrobe` SET `look` = @look, `gender` = @gender WHERE `user_id` = @id AND `slot_id` = @slot LIMIT 1",
                new { look = processedLook, gender = gender.ToUpper(), id = habbo.Id, slot = slotId });
        }
        else
        {
            connection.Execute(
                "INSERT INTO `user_wardrobe` (`user_id`,`slot_id`,`look`,`gender`) VALUES (@id,@slot,@look,@gender)",
                new { id = habbo.Id, slot = slotId, look = processedLook, gender = gender.ToUpper() });
        }

        return Task.CompletedTask;
    }

    public Task UseSellableClothing(GameClient session, uint itemId)
    {
        if (session.GetHabbo() is not { Clothing: { } clothingComponent } habbo || !habbo.TryGetCurrentRoom(out var room))
            return Task.CompletedTask;

        var item = room.GetRoomItemHandler().GetItem(itemId);
        if (item?.Definition == null)
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
        if (!_clothingManager.TryGetClothing(item.Definition.BehaviourData, out var clothing) || clothing == null)
        {
            session.SendNotification("Oops, we couldn't find this clothing part!");
            return Task.CompletedTask;
        }

        using (var connection = _database.Connection())
        {
            connection.Execute("DELETE FROM `items` WHERE `id` = @itemId LIMIT 1", new { itemId = item.Id });
        }

        room.GetRoomItemHandler().RemoveFurniture(session, item.Id);
        clothingComponent.AddClothing(clothing.ClothingName, clothing.PartIds);
        session.Send(new FigureSetIdsComposer(clothingComponent.GetClothingParts));
        session.Send(new RoomNotificationComposer("figureset.redeemed.success"));
        session.SendWhisper("If for some reason cannot see your new clothing, reload the hotel!");
        return Task.CompletedTask;
    }

    public Task SetMannequinFigure(GameClient session, uint itemId)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var room) || !room.CheckRights(session, true))
            return Task.CompletedTask;

        var item = room.GetRoomItemHandler().GetItem(itemId);
        if (item == null)
            return Task.CompletedTask;

        var gender = habbo.Gender.ToLower();
        var figure = habbo.Look.Split('.')
            .Where(str => !str.Contains("hr") && !str.Contains("hd") && !str.Contains("he") && !str.Contains("ea") && !str.Contains("ha"))
            .Aggregate(string.Empty, (current, str) => $"{current}{str}.")
            .TrimEnd('.');

        if (item.LegacyDataString.Contains(Convert.ToChar(5)))
        {
            var flags = item.LegacyDataString.Split(Convert.ToChar(5));
            item.LegacyDataString = gender + Convert.ToChar(5) + figure + Convert.ToChar(5) + flags[2];
        }
        else
        {
            item.LegacyDataString = $"{gender}{Convert.ToChar(5)}{figure}{Convert.ToChar(5)}Default";
        }

        item.UpdateState(true, true);
        return Task.CompletedTask;
    }

    public Task SetMannequinName(GameClient session, uint itemId, string name)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var room) || !room.CheckRights(session, true))
            return Task.CompletedTask;

        var item = room.GetRoomItemHandler().GetItem(itemId);
        if (item == null)
            return Task.CompletedTask;

        if (item.LegacyDataString.Contains(Convert.ToChar(5)))
        {
            var flags = item.LegacyDataString.Split(Convert.ToChar(5));
            item.LegacyDataString = flags[0] + Convert.ToChar(5) + flags[1] + Convert.ToChar(5) + name;
        }
        else
        {
            item.LegacyDataString = $"m{Convert.ToChar(5)}.ch-210-1321.lg-285-92{Convert.ToChar(5)}Default Mannequin";
        }

        using (var connection = _database.Connection())
        {
            connection.Execute(
                "UPDATE `items` SET `extra_data` = @extraData WHERE `id` = @itemId LIMIT 1",
                new { itemId = item.Id, extraData = item.LegacyDataString });
        }

        item.UpdateState(true, true);
        return Task.CompletedTask;
    }
}
