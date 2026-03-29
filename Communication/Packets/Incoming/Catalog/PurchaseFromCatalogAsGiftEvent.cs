using System.Globalization;
using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Core.Settings;
using Plus.Database;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.Catalog.Utilities;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Quests;
using Plus.Utilities;
using Dapper;

namespace Plus.Communication.Packets.Incoming.Catalog;

public class PurchaseFromCatalogAsGiftEvent : IPacketEvent
{
    private readonly ICatalogManager _catalogManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IItemDataManager _itemManager;
    private readonly IDatabase _database;
    private readonly IAchievementService _achievementService;
    private readonly IGameClientManager _gameClientManager;
    private readonly IQuestService _questService;
    private readonly IItemFactory _itemFactory;
    private readonly IPetUtility _petUtility;

    public PurchaseFromCatalogAsGiftEvent(ICatalogManager catalogManager,
        ISettingsManager settingsManager,
        IItemDataManager itemManager,
        IDatabase database,
        IAchievementService achievementService,
        IGameClientManager gameClientManager,
        IQuestService questService,
        IItemFactory itemFactory,
        IPetUtility petUtility)
    {
        _catalogManager = catalogManager;
        _settingsManager = settingsManager;
        _itemManager = itemManager;
        _database = database;
        _achievementService = achievementService;
        _gameClientManager = gameClientManager;
        _questService = questService;
        _itemFactory = itemFactory;
        _petUtility = petUtility;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { Inventory.Badges: { } senderBadges } sender)
            return;

        var pageId = packet.ReadInt();
        var itemId = packet.ReadInt();
        var data = packet.ReadString();
        var giftUser = StringCharFilter.Escape(packet.ReadString());
        var giftMessage = StringCharFilter.Escape(packet.ReadString().Replace(Convert.ToChar(5), ' '));
        var spriteId = packet.ReadInt();
        var ribbon = packet.ReadInt();
        var colour = packet.ReadInt();
        packet.ReadBool();
        if (_settingsManager.TryGetValue("room.item.gifts.enabled") != "1")
        {
            session.SendNotification("The hotel managers have disabled gifting");
            return;
        }
        if (!_catalogManager.TryGetPage(pageId, out var page))
            return;
        if (!page.Enabled || !page.Visible || page.MinimumRank > sender.Rank || page.MinimumVip > sender.VipRank && sender.Rank == 1)
            return;
        if (!page.Items.TryGetValue(itemId, out var item))
        {
            if (page.ItemOffers.ContainsKey(itemId))
            {
                item = page.ItemOffers[itemId];
                if (item == null)
                    return;
            }
            else
                return;
        }
        if (!ItemUtility.CanGiftItem(item))
            return;
        if (!_itemManager.Gifts.TryGetValue(spriteId, out var presentId) || !_itemManager.Items.TryGetValue(presentId, out var presentData) || presentData.InteractionType != InteractionType.Gift)
            return;
        if (sender.Credits < item.CostCredits)
        {
            session.Send(new PresentDeliverErrorComposer(true, false));
            return;
        }
        if (sender.Duckets < item.CostPixels)
        {
            session.Send(new PresentDeliverErrorComposer(false, true));
            return;
        }
        var receiverHabbo = _gameClientManager.GetClientByUsername(giftUser)?.GetHabbo();
        if (receiverHabbo == null)
        {
            session.Send(new GiftWrappingErrorComposer());
            return;
        }
        if (!receiverHabbo.AllowGifts)
        {
            session.SendNotification("Oops, this user doesn't allow gifts to be sent to them!");
            return;
        }
        if ((DateTime.Now - sender.LastGiftPurchaseTime).TotalSeconds <= 15.0)
        {
            session.SendNotification("You're purchasing gifts too fast! Please wait 15 seconds!");
            sender.GiftPurchasingWarnings += 1;
            if (sender.GiftPurchasingWarnings >= 25)
                sender.SessionGiftBlocked = true;
            return;
        }
        if (sender.SessionGiftBlocked)
            return;
        var extra_data = giftUser + Convert.ToChar(5) + giftMessage + Convert.ToChar(5) + sender.Id + Convert.ToChar(5) + item.Definition.Id + Convert.ToChar(5) + spriteId + Convert.ToChar(5) + ribbon +
                 Convert.ToChar(5) + colour;
        int newItemId = 0;
        using (var connection = _database.Connection())
        {
            //Insert the dummy item.
            var InsertId = await connection.ExecuteScalarAsync<int>("INSERT INTO `items` (`base_item`,`user_id`,`extra_data`) VALUES (@baseId, @habboId, @extra_data); SELECT LAST_INSERT_ID();",
                new { baseId = presentData.Id, habboId = receiverHabbo.Id, extra_data = extra_data });
            newItemId = InsertId;
            string? itemExtraData = null;
            switch (item.Definition.InteractionType)
            {
                case InteractionType.None:
                    itemExtraData = "";
                    break;
                case InteractionType.Pet:
                    try
                    {
                        var bits = data.Split('\n');
                        var petName = bits[0];
                        var race = bits[1];
                        var color = bits[2];
                        if (!_petUtility.CheckPetName(petName))
                            return;
                        if (race.Length > 2)
                            return;
                        if (color.Length != 6)
                            return;
                        await _achievementService.ProgressAchievement(session, "ACH_PetLover", 1);
                    }
                    catch
                    {
                        return;
                    }
                    break;
                case var _ when item.Definition.IsRoomDecoration:
                    double number = 0;
                    try
                    {
                        number = string.IsNullOrEmpty(data) ? 0 : double.Parse(data, PlusEnvironment.CultureInfo);
                    }
                    catch
                    {
                        //ignored
                    }
                    itemExtraData = number.ToString(CultureInfo.CurrentCulture).Replace(',', '.');
                    break; // maintain extra data // todo: validate
                case InteractionType.Postit:
                    itemExtraData = "FFFF33";
                    break;
                case var _ when item.Definition.IsMoodlight:
                    itemExtraData = "1,1,1,#000000,255";
                    break;
                case InteractionType.Trophy:
                    itemExtraData = $"{sender.Username}{Convert.ToChar(9)}{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year}{Convert.ToChar(9)}{data}";
                    break;
                case InteractionType.Mannequin:
                    itemExtraData = $"m{Convert.ToChar(5)}.ch-210-1321.lg-285-92{Convert.ToChar(5)}Default Mannequin";
                    break;
                case InteractionType.BadgeDisplay:
                    if (!senderBadges.HasBadge(data))
                    {
                        session.Send(new BroadcastMessageAlertComposer("Oops, it appears that you do not own this badge."));
                        return;
                    }
                    itemExtraData = $"{data}{Convert.ToChar(9)}{sender.Username}{Convert.ToChar(9)}{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year}";
                    break;
                default:
                    itemExtraData = data;
                    break;
            }

            //Insert the present, forever.
            await connection.ExecuteAsync("INSERT INTO `user_presents` (`item_id`,`base_id`,`extra_data`) VALUES (@itemId, @baseId, @extra_data)",
                new {itemId = newItemId, baseId = item.Definition.Id, extra_data = string.IsNullOrEmpty(itemExtraData) ? "" : itemExtraData });

            //Here we're clearing up a record, this is dumb, but okay.
            await connection.ExecuteAsync("DELETE FROM `items` WHERE `id` = @deleteId LIMIT 1", new { deleteId = newItemId});
        }
        var giveItem = _itemFactory.CreateGiftItem(presentData, receiverHabbo, extra_data, extra_data, newItemId).ToInventoryItem();
        if (giveItem != null)
        {
            var receiver = _gameClientManager.GetClientByUserId(receiverHabbo.Id);
            var receiverFurniture = receiver?.GetHabbo()?.Inventory?.Furniture;
            if (receiver != null)
            {
                if (receiverFurniture == null)
                    return;
                receiverFurniture.AddItem(giveItem);
                receiver.Send(new FurniListNotificationComposer(giveItem.Id, 1));
                receiver.Send(new PurchaseOkComposer());
                receiver.Send(new FurniListAddComposer(giveItem));
                receiver.Send(new FurniListUpdateComposer());
            }

            if (receiverHabbo.Id != sender.Id)
            {
                await _achievementService.ProgressAchievement(session, "ACH_GiftGiver", 1);
                if (receiver != null)
                    await _achievementService.ProgressAchievement(receiver, "ACH_GiftReceiver", 1);
                await _questService.ProgressUserQuest(session, QuestType.GiftOthers);
            }
        }
        session.Send(new PurchaseOkComposer(item, presentData));
        if (item.CostCredits > 0)
        {
            sender.Credits -= item.CostCredits;
            session.Send(new CreditBalanceComposer(sender.Credits));
        }
        if (item.CostPixels > 0)
        {
            sender.Duckets -= item.CostPixels;
            session.Send(new HabboActivityPointNotificationComposer(sender.Duckets, sender.Duckets));
        }
        sender.LastGiftPurchaseTime = DateTime.Now;
    }
}
