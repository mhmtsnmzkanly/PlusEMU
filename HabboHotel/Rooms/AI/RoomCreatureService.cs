using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Inventory.Bots;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Inventory.Pets;
using Plus.Communication.Packets.Outgoing.Pets;
using Plus.Communication.Packets.Outgoing.Rooms.AI.Bots;
using Plus.Communication.Packets.Outgoing.Rooms.AI.Pets;
using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.Core.Settings;
using Plus.Database;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Catalog.Utilities;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms.AI.Speech;
using Plus.HabboHotel.Rooms.Chat.Pets.Locale;
using Plus.Utilities;

namespace Plus.HabboHotel.Rooms.AI;

internal class RoomCreatureService : IRoomCreatureService
{
    private readonly ILogger<RoomCreatureService> _logger;
    private readonly IRoomManager _roomManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IGameClientManager _clientManager;
    private readonly IDatabase _database;
    private readonly IAchievementManager _achievementManager;
    private readonly IQuestManager _questManager;
    private readonly IPetLocale _petLocale;
    private readonly IItemDataManager _itemDataManager;
    private readonly IItemFactory _itemFactory;

    public RoomCreatureService(
        IRoomManager roomManager,
        ISettingsManager settingsManager,
        ILogger<RoomCreatureService> logger,
        IGameClientManager clientManager,
        IDatabase database,
        IAchievementManager achievementManager,
        IQuestManager questManager,
        IPetLocale petLocale,
        IItemDataManager itemDataManager,
        IItemFactory itemFactory)
    {
        _roomManager = roomManager;
        _settingsManager = settingsManager;
        _logger = logger;
        _clientManager = clientManager;
        _database = database;
        _achievementManager = achievementManager;
        _questManager = questManager;
        _petLocale = petLocale;
        _itemDataManager = itemDataManager;
        _itemFactory = itemFactory;
    }

    public Task PlacePet(Room room, GameClient session, int petId, int x, int y)
    {
        var habbo = session.GetHabbo();
        var petInventory = habbo?.Inventory?.Pets;
        if (petInventory == null)
            return Task.CompletedTask;

        if (room.AllowPets == false && !room.CheckRights(session, true) || !room.CheckRights(session, true))
        {
            session.Send(new RoomErrorNotifComposer(1));
            return Task.CompletedTask;
        }
        if (room.GetRoomUserManager().PetCount > Convert.ToInt32(_settingsManager.TryGetValue("room.pets.placement_limit")))
        {
            session.Send(new RoomErrorNotifComposer(2));
            return Task.CompletedTask;
        }
        if (!petInventory.Pets.TryGetValue(petId, out var pet))
            return Task.CompletedTask;
        if (pet.PlacedInRoom)
        {
            session.SendNotification("This pet is already in the room?");
            return Task.CompletedTask;
        }
        if (!room.GetGameMap().CanWalk(x, y, false))
        {
            session.Send(new RoomErrorNotifComposer(4));
            return Task.CompletedTask;
        }

        if (room.GetRoomUserManager().TryGetPet(pet.PetId, out var oldPet))
            room.GetRoomUserManager().RemoveBot(oldPet.VirtualId, false);

        pet.X = x;
        pet.Y = y;
        pet.PlacedInRoom = true;
        pet.RoomId = room.RoomId;
        var rndSpeechList = new List<RandomSpeech>();
        var roomBot = new RoomBot(pet.PetId, pet.RoomId, "pet", "freeroam", pet.Name, "", pet.Look, x, y, 0, 0, 0, 0, 0, 0, ref rndSpeechList, "", 0, pet.OwnerId, false, 0, false, 0);
        room.GetRoomUserManager().DeployBot(roomBot, pet);
        pet.DbState = PetDatabaseUpdateState.NeedsUpdate;
        room.GetRoomUserManager().UpdatePets();
        petInventory.RemovePet(pet.PetId);
        session.Send(new PetInventoryComposer(petInventory.Pets.Values.ToList()));
        return Task.CompletedTask;
    }

    public Task PickUpPet(Room room, GameClient session, int petId)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        if (!room.GetRoomUserManager().TryGetPet(petId, out var pet))
        {
            if (!room.CheckRights(session) && room.WhoCanKick != 2 && room.Group == null || room.Group != null && !room.CheckRights(session, false, true))
                return Task.CompletedTask;

            var targetUser = habbo.CurrentRoom?.GetRoomUserManager().GetRoomUserByHabbo(petId);
            var targetHabbo = targetUser?.GetClient()?.GetHabbo();
            if (targetUser == null || targetHabbo == null)
                return Task.CompletedTask;

            targetHabbo.PetId = 0;
            room.SendPacket(new UserRemoveComposer(targetUser.VirtualId));
            room.SendPacket(new UsersComposer(targetUser));
            return Task.CompletedTask;
        }

        if (habbo.Id != pet.PetData.OwnerId && !room.CheckRights(session, true))
        {
            session.SendWhisper("You can only pickup your own pets, to kick a pet you must have room rights.");
            return Task.CompletedTask;
        }

        if (pet.RidingHorse)
        {
            var userRiding = room.GetRoomUserManager().GetRoomUserByVirtualId(pet.HorseId);
            if (userRiding != null)
            {
                userRiding.RidingHorse = false;
                userRiding.ApplyEffect(-1);
                userRiding.MoveTo(new(userRiding.X + 1, userRiding.Y + 1));
            }
            else
            {
                pet.RidingHorse = false;
            }
        }

        var data = pet.PetData;
        if (data == null)
            return Task.CompletedTask;

        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.RunQuery($"UPDATE `bots` SET `room_id` = '0', `x` = '0', `Y` = '0', `Z` = '0' WHERE `id` = '{data.PetId}' LIMIT 1");
            dbClient.RunQuery($"UPDATE `bots_petdata` SET `experience` = '{data.Experience}', `energy` = '{data.Energy}', `nutrition` = '{data.Nutrition}', `respect` = '{data.Respect}' WHERE `id` = '{data.PetId}' LIMIT 1");
        }

        if (data.OwnerId != habbo.Id)
        {
            var target = _clientManager.GetClientByUserId(data.OwnerId);
            var targetPets = target?.GetHabbo()?.Inventory?.Pets;
            if (target != null && targetPets != null && targetPets.AddPet(pet.PetData))
            {
                pet.PetData.RoomId = 0;
                pet.PetData.PlacedInRoom = false;
                room.GetRoomUserManager().RemoveBot(pet.VirtualId, false);
                target.Send(new PetInventoryComposer(targetPets.Pets.Values.ToList()));
                return Task.CompletedTask;
            }
        }

        room.GetRoomUserManager().RemoveBot(pet.VirtualId, false);
        return Task.CompletedTask;
    }

    public Task RespectPet(Room room, GameClient session, int petId)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null || !habbo.InRoom || habbo.HabboStats.DailyPetRespectPoints == 0)
            return Task.CompletedTask;
        var currentRoom = habbo.CurrentRoom;
        var thisUser = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (currentRoom == null || thisUser == null)
            return Task.CompletedTask;

        if (!currentRoom.GetRoomUserManager().TryGetPet(petId, out var pet))
        {
            var targetUser = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(petId);
            var targetClient = targetUser?.GetClient();
            var targetHabbo = targetClient?.GetHabbo();
            if (targetUser == null || targetHabbo?.HabboStats == null)
                return Task.CompletedTask;
            if (targetHabbo.Id == habbo.Id)
            {
                session.SendWhisper("Oops, you cannot use this on yourself! (You haven't lost a point, simply reload!)");
                return Task.CompletedTask;
            }

            _questManager.ProgressUserQuest(session, QuestType.SocialRespect);
            _achievementManager.ProgressAchievement(session, "ACH_RespectGiven", 1);
            _achievementManager.ProgressAchievement(targetClient!, "ACH_RespectEarned", 1);
            habbo.HabboStats.DailyPetRespectPoints -= 1;
            habbo.HabboStats.RespectGiven += 1;
            targetHabbo.HabboStats.Respect += 1;
            thisUser.CarryItemId = 999999999;
            thisUser.CarryTimer = 5;
            if (room.RespectNotificationsEnabled)
                room.SendPacket(new RespectPetNotificationComposer(targetHabbo, targetUser));
            room.SendPacket(new CarryObjectComposer(thisUser.VirtualId, thisUser.CarryItemId));
            return Task.CompletedTask;
        }

        if (pet.PetData == null || pet.RoomId != currentRoom.RoomId)
            return Task.CompletedTask;

        habbo.HabboStats.DailyPetRespectPoints -= 1;
        _achievementManager.ProgressAchievement(session, "ACH_PetRespectGiver", 1);
        thisUser.CarryItemId = 999999999;
        thisUser.CarryTimer = 5;
        pet.PetData.OnRespect();
        room.SendPacket(new CarryObjectComposer(thisUser.VirtualId, thisUser.CarryItemId));
        return Task.CompletedTask;
    }

    public Task GetPetInformation(GameClient session, int petId)
    {
        var currentRoom = session.GetHabbo()?.CurrentRoom;
        if (currentRoom == null)
            return Task.CompletedTask;

        if (!currentRoom.GetRoomUserManager().TryGetPet(petId, out var pet))
        {
            var userHabbo = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(petId)?.GetClient()?.GetHabbo();
            if (userHabbo != null)
                session.Send(new PetInformationComposer(userHabbo));
            return Task.CompletedTask;
        }

        if (pet.RoomId == currentRoom.RoomId && pet.PetData != null)
            session.Send(new PetInformationComposer(pet.PetData));
        return Task.CompletedTask;
    }

    public Task GetPetTrainingPanel(GameClient session, int petId)
    {
        var currentRoom = session.GetHabbo()?.CurrentRoom;
        if (currentRoom == null)
            return Task.CompletedTask;

        if (!currentRoom.GetRoomUserManager().TryGetPet(petId, out var pet))
        {
            if (currentRoom.GetRoomUserManager().GetRoomUserByHabbo(petId)?.GetClient()?.GetHabbo() != null)
                session.SendWhisper("Maybe one day, boo boo.");
            return Task.CompletedTask;
        }

        if (pet.RoomId == currentRoom.RoomId && pet.PetData != null)
            session.Send(new PetTrainingPanelComposer(pet.PetData.PetId, pet.PetData.Level));
        return Task.CompletedTask;
    }

    public Task RideHorse(Room room, GameClient session, int petId, bool mount)
    {
        var habbo = session.GetHabbo();
        var user = habbo == null ? null : room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (habbo == null || user == null || !room.GetRoomUserManager().TryGetPet(petId, out var pet) || pet.PetData == null)
            return Task.CompletedTask;
        if (pet.PetData.AnyoneCanRide == 0 && pet.PetData.OwnerId != user.UserId)
        {
            session.SendNotification("You are unable to ride this horse.\nThe owner of the pet has not selected for anyone to ride it.");
            return Task.CompletedTask;
        }

        if (mount)
        {
            if (pet.RidingHorse)
            {
                var speech = _petLocale.GetValue("pet.alreadymounted");
                pet.Chat(speech[Random.Shared.Next(0, speech.Length)]);
            }
            else if (user.RidingHorse)
            {
                session.SendNotification("You are already riding a horse!");
            }
            else
            {
                if (pet.Statusses.Count > 0)
                    pet.Statusses.Clear();
                var newX = user.X;
                var newY = user.Y;
                room.SendPacket(room.GetRoomItemHandler().UpdateUserOnRoller(pet, new(newX, newY), 0, room.GetGameMap().SqAbsoluteHeight(newX, newY)));
                room.SendPacket(room.GetRoomItemHandler().UpdateUserOnRoller(user, new(newX, newY), 0, room.GetGameMap().SqAbsoluteHeight(newX, newY) + 1));
                user.MoveTo(newX, newY);
                pet.ClearMovement(true);
                user.RidingHorse = true;
                pet.RidingHorse = true;
                pet.HorseId = user.VirtualId;
                user.HorseId = pet.VirtualId;
                user.ApplyEffect(77);
                user.RotBody = pet.RotBody;
                user.RotHead = pet.RotHead;
                user.UpdateNeeded = true;
                pet.UpdateNeeded = true;
            }
        }
        else
        {
            if (user.VirtualId == pet.HorseId)
            {
                pet.Statusses.Remove("sit");
                pet.Statusses.Remove("lay");
                pet.Statusses.Remove("snf");
                pet.Statusses.Remove("eat");
                pet.Statusses.Remove("ded");
                pet.Statusses.Remove("jmp");
                user.RidingHorse = false;
                user.HorseId = 0;
                pet.RidingHorse = false;
                pet.HorseId = 0;
                user.MoveTo(new(user.X + 2, user.Y + 2));
                user.ApplyEffect(-1);
                user.UpdateNeeded = true;
                pet.UpdateNeeded = true;
            }
            else
            {
                session.SendNotification("Could not dismount this horse - You are not riding it!");
            }
        }

        room.SendPacket(new PetHorseFigureInformationComposer(pet));
        return Task.CompletedTask;
    }

    public Task ApplyHorseEffect(Room room, GameClient session, uint itemId, int petId)
    {
        var habbo = session.GetHabbo();
        var item = room.GetRoomItemHandler().GetItem(itemId);
        if (habbo == null || item == null || !room.GetRoomUserManager().TryGetPet(petId, out var petUser) || petUser.PetData == null || petUser.PetData.OwnerId != habbo.Id)
            return Task.CompletedTask;

        if (item.Definition.InteractionType == InteractionType.HorseSaddle1)
        {
            petUser.PetData.Saddle = 9;
            UpdateHorsePetAndConsumeItem(petUser.PetData.PetId, item.Id, room, session, item, "have_saddle", "9");
        }
        else if (item.Definition.InteractionType == InteractionType.HorseSaddle2)
        {
            petUser.PetData.Saddle = 10;
            UpdateHorsePetAndConsumeItem(petUser.PetData.PetId, item.Id, room, session, item, "have_saddle", "10");
        }
        else if (item.Definition.InteractionType == InteractionType.HorseHairstyle)
        {
            var parse = 100 + int.Parse(item.Definition.ItemName.Split('_')[2]);
            petUser.PetData.PetHair = parse;
            UpdateHorsePetAndConsumeItem(petUser.PetData.PetId, item.Id, room, session, item, "pethair", petUser.PetData.PetHair.ToString());
        }
        else if (item.Definition.InteractionType == InteractionType.HorseHairDye)
        {
            var hairDye = 48 + int.Parse(item.Definition.ItemName.Split('_')[2]);
            petUser.PetData.HairDye = hairDye;
            UpdateHorsePetAndConsumeItem(petUser.PetData.PetId, item.Id, room, session, item, "hairdye", petUser.PetData.HairDye.ToString());
        }
        else if (item.Definition.InteractionType == InteractionType.HorseBodyDye)
        {
            var parse = int.Parse(item.Definition.ItemName.Split('_')[2]);
            var raceLast = 2 + parse * 4 - 4;
            if (parse == 13) raceLast = 61;
            else if (parse == 14) raceLast = 65;
            else if (parse == 15) raceLast = 69;
            else if (parse == 16) raceLast = 73;
            petUser.PetData.Race = raceLast.ToString();
            UpdateHorsePetAndConsumeItem(petUser.PetData.PetId, item.Id, room, session, item, "race", petUser.PetData.Race);
        }

        room.SendPacket(new UsersComposer(petUser));
        room.SendPacket(new PetHorseFigureInformationComposer(petUser));
        return Task.CompletedTask;
    }

    public Task RemoveSaddleFromHorse(GameClient session, int petId)
    {
        var habbo = session.GetHabbo();
        var currentRoom = habbo?.CurrentRoom;
        if (habbo == null || currentRoom == null || !_roomManager.TryGetRoom(currentRoom.Id, out var room))
            return Task.CompletedTask;
        if (!room.GetRoomUserManager().TryGetPet(petId, out var petUser) || petUser.PetData == null || petUser.PetData.OwnerId != habbo.Id)
            return Task.CompletedTask;

        var saddleId = ItemUtility.GetSaddleId(petUser.PetData.Saddle);
        petUser.PetData.Saddle = 0;
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.RunQuery($"UPDATE `bots_petdata` SET `have_saddle` = '0' WHERE `id` = '{petUser.PetData.PetId}' LIMIT 1");
        }

        if (!_itemDataManager.Items.TryGetValue(saddleId, out var itemData))
            return Task.CompletedTask;
        var item = _itemFactory.CreateSingleItemNullable(itemData, habbo, "", "").ToInventoryItem();
        if (item != null)
        {
            habbo.Inventory?.Furniture?.AddItem(item);
            session.Send(new FurniListNotificationComposer(item.Id, 1));
            session.Send(new PurchaseOkComposer());
            session.Send(new FurniListAddComposer(item));
            session.Send(new FurniListUpdateComposer());
        }

        room.SendPacket(new UsersComposer(petUser));
        room.SendPacket(new PetHorseFigureInformationComposer(petUser));
        return Task.CompletedTask;
    }

    public Task PlaceBot(Room room, GameClient session, int botId, int x, int y)
    {
        var habbo = session.GetHabbo();
        var inventory = habbo?.Inventory;
        if (habbo == null || !room.CheckRights(session, true))
            return Task.CompletedTask;
        if (!room.GetGameMap().CanWalk(x, y, false) || !room.GetGameMap().ValidTile(x, y))
        {
            session.SendNotification("You cannot place a bot here!");
            return Task.CompletedTask;
        }
        if (inventory?.Bots == null || !inventory.Bots.Bots.TryGetValue(botId, out var bot))
            return Task.CompletedTask;

        var botCount = room.GetRoomUserManager().GetUserList().Count(user => user != null && !user.IsPet && user.IsBot);
        if (botCount >= 5 && !(habbo.Permissions?.HasRight("bot_place_any_override") ?? false))
        {
            session.SendNotification("Sorry; 5 bots per room only!");
            return Task.CompletedTask;
        }

        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery("UPDATE `bots` SET `room_id` = @roomId, `x` = @CoordX, `y` = @CoordY WHERE `id` = @BotId LIMIT 1");
            dbClient.AddParameter("roomId", room.RoomId);
            dbClient.AddParameter("BotId", bot.Id);
            dbClient.AddParameter("CoordX", x);
            dbClient.AddParameter("CoordY", y);
            dbClient.RunQuery();
        }

        var botSpeechList = new List<RandomSpeech>();
        DataRow? getData;
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery("SELECT `ai_type`,`rotation`,`walk_mode`,`automatic_chat`,`speaking_interval`,`mix_sentences`,`chat_bubble` FROM `bots` WHERE `id` = @BotId LIMIT 1");
            dbClient.AddParameter("BotId", bot.Id);
            getData = dbClient.GetRow();
            dbClient.SetQuery("SELECT `text` FROM `bots_speech` WHERE `bot_id` = @BotId");
            dbClient.AddParameter("BotId", bot.Id);
            var botSpeech = dbClient.GetTable();
            if (botSpeech != null)
                foreach (DataRow speech in botSpeech.Rows)
                    botSpeechList.Add(new(Convert.ToString(speech["text"]) ?? string.Empty, bot.Id));
        }

        if (getData == null)
            return Task.CompletedTask;

        var botUser = room.GetRoomUserManager().DeployBot(
            new(bot.Id, room.RoomId, Convert.ToString(getData["ai_type"]) ?? string.Empty, Convert.ToString(getData["walk_mode"]) ?? string.Empty, bot.Name, "", bot.Figure, x, y, 0, 4, 0, 0, 0, 0,
                ref botSpeechList, "", 0, bot.OwnerId, ConvertExtensions.EnumToBool(getData["automatic_chat"].ToString() ?? "0"), Convert.ToInt32(getData["speaking_interval"]),
                ConvertExtensions.EnumToBool(getData["mix_sentences"].ToString() ?? "0"), Convert.ToInt32(getData["chat_bubble"])), null!);
        botUser.Chat("Hello!");
        room.GetGameMap().UpdateUserMovement(new(x, y), new(x, y), botUser);
        if (!inventory.Bots.RemoveBot(botId))
        {
            _logger.LogError("Error whilst removing Bot: {BotId}", bot.Id);
            return Task.CompletedTask;
        }

        session.Send(new BotInventoryComposer(inventory.Bots.Bots.Values.ToList()));
        return Task.CompletedTask;
    }

    public Task PickUpBot(GameClient session, int botId)
    {
        var habbo = session.GetHabbo();
        var room = habbo?.CurrentRoom;
        if (habbo == null || room == null || botId == 0)
            return Task.CompletedTask;
        if (!room.GetRoomUserManager().TryGetBot(botId, out var botUser) || botUser.BotData == null)
            return Task.CompletedTask;
        if (habbo.Id != botUser.BotData.OwnerId && !(habbo.Permissions?.HasRight("bot_place_any_override") ?? false))
        {
            session.SendWhisper("You can only pick up your own bots!");
            return Task.CompletedTask;
        }

        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery("UPDATE `bots` SET `room_id` = '0' WHERE `id` = @id LIMIT 1");
            dbClient.AddParameter("id", botId);
            dbClient.RunQuery();
        }

        room.GetGameMap().RemoveUserFromMap(botUser, new(botUser.X, botUser.Y));
        if (habbo.Inventory?.Bots == null)
            return Task.CompletedTask;

        habbo.Inventory.Bots.AddBot(new(Convert.ToInt32(botUser.BotData.Id), Convert.ToInt32(botUser.BotData.OwnerId), botUser.BotData.Name, botUser.BotData.Motto, botUser.BotData.Look, botUser.BotData.Gender));
        session.Send(new BotInventoryComposer(habbo.Inventory.Bots.Bots.Values.ToList()));
        room.GetRoomUserManager().RemoveBot(botUser.VirtualId, false);
        return Task.CompletedTask;
    }

    public Task OpenBotAction(GameClient session, int botId, int actionId)
    {
        var room = session.GetHabbo()?.CurrentRoom;
        if (room == null || !room.GetRoomUserManager().TryGetBot(botId, out var botUser))
            return Task.CompletedTask;

        var botSpeech = string.Join('\n', botUser.BotData.RandomSpeech.Select(s => s.Message)) + "\n;#;" +
                        botUser.BotData.AutomaticChat + ";#;" +
                        botUser.BotData.SpeakingInterval + ";#;" +
                        botUser.BotData.MixSentences;
        if (actionId == 2 || actionId == 5)
            session.Send(new OpenBotActionComposer(botUser, actionId, botSpeech));
        return Task.CompletedTask;
    }

    public Task SaveBotAction(GameClient session, int botId, int actionId, string dataString)
    {
        var habbo = session.GetHabbo();
        var room = habbo?.CurrentRoom;
        if (habbo == null || room == null || actionId < 1 || actionId > 5)
            return Task.CompletedTask;
        if (!room.GetRoomUserManager().TryGetBot(botId, out var bot) || bot.BotData == null)
            return Task.CompletedTask;
        if (bot.BotData.OwnerId != habbo.Id && !(habbo.Permissions?.HasRight("bot_edit_any_override") ?? false))
            return Task.CompletedTask;

        var roomBot = bot.BotData;
        switch (actionId)
        {
            case 1:
                bot.BotData.Look = habbo.Look;
                bot.BotData.Gender = habbo.Gender;
                room.SendPacket(new UserChangeComposer(bot.BotData));
                using (var dbClient = _database.GetQueryReactor())
                {
                    dbClient.SetQuery($"UPDATE `bots` SET `look` = @look, `gender` = '{habbo.Gender}' WHERE `id` = '{bot.BotData.Id}' LIMIT 1");
                    dbClient.AddParameter("look", habbo.Look);
                    dbClient.RunQuery();
                }
                break;
            case 2:
                SaveBotSpeech(botId, roomBot, dataString);
                break;
            case 3:
                roomBot.WalkingMode = roomBot.WalkingMode == "stand" ? "freeroam" : "stand";
                using (var dbClient = _database.GetQueryReactor())
                    dbClient.RunQuery($"UPDATE `bots` SET `walk_mode` = '{roomBot.WalkingMode}' WHERE `id` = '{roomBot.Id}' LIMIT 1");
                break;
            case 4:
                roomBot.DanceId = roomBot.DanceId > 0 ? 0 : Random.Shared.Next(1, 4);
                room.SendPacket(new DanceComposer(bot, roomBot.DanceId));
                break;
            case 5:
                if (dataString.Length == 0)
                {
                    session.SendWhisper("Come on, atleast give the bot a name!");
                    return Task.CompletedTask;
                }
                if (dataString.Length >= 16)
                {
                    session.SendWhisper("Come on, the bot doesn't need a name that long!");
                    return Task.CompletedTask;
                }
                if (dataString.Contains("<img src") || dataString.Contains("<font ") || dataString.Contains("</font>") || dataString.Contains("</a>") || dataString.Contains("<i>"))
                {
                    session.SendWhisper("No HTML, please :<");
                    return Task.CompletedTask;
                }
                roomBot.Name = dataString;
                using (var dbClient = _database.GetQueryReactor())
                {
                    dbClient.SetQuery($"UPDATE `bots` SET `name` = @name WHERE `id` = '{roomBot.Id}' LIMIT 1");
                    dbClient.AddParameter("name", dataString);
                    dbClient.RunQuery();
                }
                room.SendPacket(new UsersComposer(bot));
                break;
        }
        return Task.CompletedTask;
    }

    private void SaveBotSpeech(int botId, RoomBot roomBot, string dataString)
    {
        var configData = dataString.Split(new[] { ";#;" }, StringSplitOptions.None);
        var speechData = configData[0].Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var automaticChat = Convert.ToString(configData[1]) ?? string.Empty;
        var speakingInterval = Convert.ToString(configData[2]) ?? string.Empty;
        var mixChat = Convert.ToString(configData[3]) ?? string.Empty;
        if (string.IsNullOrEmpty(speakingInterval) || Convert.ToInt32(speakingInterval) <= 0 || Convert.ToInt32(speakingInterval) < 7)
            speakingInterval = "7";
        roomBot.AutomaticChat = Convert.ToBoolean(automaticChat);
        roomBot.SpeakingInterval = Convert.ToInt32(speakingInterval);
        roomBot.MixSentences = Convert.ToBoolean(mixChat);

        using var dbClient = _database.GetQueryReactor();
        dbClient.RunQuery($"DELETE FROM `bots_speech` WHERE `bot_id` = '{roomBot.Id}'");
        for (var i = 0; i <= speechData.Length - 1; i++)
        {
            speechData[i] = Regex.Replace(speechData[i], "<(.|\\n)*?>", string.Empty);
            dbClient.SetQuery("INSERT INTO `bots_speech` (`bot_id`, `text`) VALUES (@id, @data)");
            dbClient.AddParameter("id", botId);
            dbClient.AddParameter("data", speechData[i]);
            dbClient.RunQuery();
            dbClient.SetQuery("UPDATE `bots` SET `automatic_chat` = @AutomaticChat, `speaking_interval` = @SpeakingInterval, `mix_sentences` = @MixChat WHERE `id` = @id LIMIT 1");
            dbClient.AddParameter("id", botId);
            dbClient.AddParameter("AutomaticChat", automaticChat.ToLower());
            dbClient.AddParameter("SpeakingInterval", Convert.ToInt32(speakingInterval));
            dbClient.AddParameter("MixChat", ConvertExtensions.ToStringEnumValue(roomBot.MixSentences));
            dbClient.RunQuery();
        }

        roomBot.RandomSpeech.Clear();
        dbClient.SetQuery("SELECT `text` FROM `bots_speech` WHERE `bot_id` = @id");
        dbClient.AddParameter("id", botId);
        var botSpeech = dbClient.GetTable();
        if (botSpeech != null)
            foreach (DataRow speech in botSpeech.Rows)
                roomBot.RandomSpeech.Add(new(Convert.ToString(speech["text"]) ?? string.Empty, botId));
    }

    private void UpdateHorsePetAndConsumeItem(int petId, uint itemId, Room room, GameClient session, Item item, string field, string value)
    {
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.RunQuery($"UPDATE `bots_petdata` SET `{field}` = '{value}' WHERE `id` = '{petId}' LIMIT 1");
            dbClient.RunQuery($"DELETE FROM `items` WHERE `id` = '{item.Id}' LIMIT 1");
        }
        room.GetRoomItemHandler().RemoveFurniture(session, itemId);
    }
}
