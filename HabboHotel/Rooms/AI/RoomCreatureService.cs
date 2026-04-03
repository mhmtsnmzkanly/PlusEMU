using System.Text.RegularExpressions;
using Dapper;
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
using Plus.Core.Language;
using Plus.Core.Settings;
using Plus.Database;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Catalog.Utilities;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms.AI.Speech;
using Plus.HabboHotel.Rooms.Chat.Pets.Locale;
using Plus.HabboHotel.Groups;
using Plus.Utilities;

namespace Plus.HabboHotel.Rooms.AI;

internal class RoomCreatureService : IRoomCreatureService
{
    private sealed class BotPlacementRow
    {
        public string AiType { get; init; } = string.Empty;
        public int Rotation { get; init; }
        public string WalkMode { get; init; } = string.Empty;
        public string AutomaticChat { get; init; } = string.Empty;
        public int SpeakingInterval { get; init; }
        public string MixSentences { get; init; } = string.Empty;
        public int ChatBubble { get; init; }
    }

    private sealed class BotSpeechRow
    {
        public string Text { get; init; } = string.Empty;
    }

    private readonly ILogger<RoomCreatureService> _logger;
    private readonly IRoomManager _roomManager;
    private readonly ILanguageManager _languageManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IGameClientManager _clientManager;
    private readonly IDatabase _database;
    private readonly IAchievementService _achievementService;
    private readonly IQuestService _questService;
    private readonly IPetLocale _petLocale;
    private readonly IItemDataManager _itemDataManager;
    private readonly IItemFactory _itemFactory;
    private readonly IGroupManager _groupManager;

    public RoomCreatureService(
        IRoomManager roomManager,
        ILanguageManager languageManager,
        ISettingsManager settingsManager,
        ILogger<RoomCreatureService> logger,
        IGameClientManager clientManager,
        IDatabase database,
        IAchievementService achievementService,
        IQuestService questService,
        IPetLocale petLocale,
        IItemDataManager itemDataManager,
        IItemFactory itemFactory,
        IGroupManager groupManager)
    {
        _roomManager = roomManager;
        _languageManager = languageManager;
        _settingsManager = settingsManager;
        _logger = logger;
        _clientManager = clientManager;
        _database = database;
        _achievementService = achievementService;
        _questService = questService;
        _petLocale = petLocale;
        _itemDataManager = itemDataManager;
        _itemFactory = itemFactory;
        _groupManager = groupManager;
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
        if (room.GetRoomUserManager().PetCount > _settingsManager.GetIntOrDefault("room.pets.placement_limit", 0))
        {
            session.Send(new RoomErrorNotifComposer(2));
            return Task.CompletedTask;
        }
        if (!petInventory.Pets.TryGetValue(petId, out var pet))
            return Task.CompletedTask;
        if (pet.PlacedInRoom)
        {
            session.SendNotification(_languageManager.Require("pet.already_in_room"));
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

            var targetUser = room.GetRoomUserManager().GetRoomUserByHabbo(petId);
            var targetHabbo = targetUser?.GetClient()?.GetHabbo();
            if (targetUser == null || targetHabbo == null)
                return Task.CompletedTask;

            targetHabbo.PetId = 0;
            room.SendPacket(new UserRemoveComposer(targetUser.VirtualId));
            room.SendPacket(new UsersComposer(targetUser, _groupManager, room.GetCacheManager()));
            return Task.CompletedTask;
        }

        if (habbo.Id != pet.PetData.OwnerId && !room.CheckRights(session, true))
        {
            session.SendWhisper(_languageManager.Require("pet.pickup.owner_only"));
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

        using (var connection = _database.Connection())
        {
            connection.Execute(
                "UPDATE `bots` SET `room_id` = 0, `x` = 0, `Y` = 0, `Z` = 0 WHERE `id` = @petId LIMIT 1",
                new { petId = data.PetId });
            connection.Execute(
                "UPDATE `bots_petdata` SET `experience` = @experience, `energy` = @energy, `nutrition` = @nutrition, `respect` = @respect WHERE `id` = @petId LIMIT 1",
                new
                {
                    petId = data.PetId,
                    experience = data.Experience,
                    energy = data.Energy,
                    nutrition = data.Nutrition,
                    respect = data.Respect
                });
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

    public async Task RespectPet(Room room, GameClient session, int petId)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null || !habbo.TryGetCurrentRoom(out var currentRoom) || habbo.HabboStats.DailyPetRespectPoints == 0)
            return;
        var thisUser = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (currentRoom == null || thisUser == null)
            return;

        if (!currentRoom.GetRoomUserManager().TryGetPet(petId, out var pet))
        {
            var targetUser = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(petId);
            var targetClient = targetUser?.GetClient();
            var targetHabbo = targetClient?.GetHabbo();
            if (targetUser == null || targetHabbo?.HabboStats == null)
                return;
            if (targetHabbo.Id == habbo.Id)
            {
                session.SendWhisper(_languageManager.Require("pet.respect.self_disallowed"));
                return;
            }

            await _questService.ProgressUserQuest(session, QuestType.SocialRespect);
            await _achievementService.ProgressAchievement(session, "ACH_RespectGiven", 1);
            await _achievementService.ProgressAchievement(targetClient!, "ACH_RespectEarned", 1);
            habbo.HabboStats.DailyPetRespectPoints -= 1;
            habbo.HabboStats.RespectGiven += 1;
            targetHabbo.HabboStats.Respect += 1;
            thisUser.CarryItemId = 999999999;
            thisUser.CarryTimer = 5;
            if (room.RespectNotificationsEnabled)
                room.SendPacket(new RespectPetNotificationComposer(targetHabbo, targetUser));
            room.SendPacket(new CarryObjectComposer(thisUser.VirtualId, thisUser.CarryItemId));
            return;
        }

        if (pet.PetData == null || pet.RoomId != currentRoom.RoomId)
            return;

        habbo.HabboStats.DailyPetRespectPoints -= 1;
        await _achievementService.ProgressAchievement(session, "ACH_PetRespectGiver", 1);
        thisUser.CarryItemId = 999999999;
        thisUser.CarryTimer = 5;
        pet.PetData.OnRespect();
        room.SendPacket(new CarryObjectComposer(thisUser.VirtualId, thisUser.CarryItemId));
    }

    public Task GetPetInformation(GameClient session, int petId)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var currentRoom))
            return Task.CompletedTask;

        if (!currentRoom.GetRoomUserManager().TryGetPet(petId, out var pet))
        {
            var userHabbo = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(petId)?.GetClient()?.GetHabbo();
            if (userHabbo != null)
                session.Send(new PetInformationComposer(userHabbo, _roomManager));
            return Task.CompletedTask;
        }

        if (pet.RoomId == currentRoom.RoomId && pet.PetData != null)
            session.Send(new PetInformationComposer(pet.PetData, _roomManager));
        return Task.CompletedTask;
    }

    public Task GetPetTrainingPanel(GameClient session, int petId)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var currentRoom))
            return Task.CompletedTask;

        if (!currentRoom.GetRoomUserManager().TryGetPet(petId, out var pet))
        {
            if (currentRoom.GetRoomUserManager().GetRoomUserByHabbo(petId)?.GetClient()?.GetHabbo() != null)
                session.SendWhisper(_languageManager.Require("pet.training.habbo_only"));
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
            session.SendNotification(_languageManager.Require("pet.ride.disallowed"));
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
                session.SendNotification(_languageManager.Require("pet.ride.already_riding"));
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
                session.SendNotification(_languageManager.Require("pet.ride.dismount_failed"));
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

        if (item.Definition.IsHorseSaddle1)
        {
            petUser.PetData.Saddle = 9;
            UpdateHorsePetAndConsumeItem(petUser.PetData.PetId, item.Id, room, session, item, "have_saddle", "9");
        }
        else if (item.Definition.IsHorseSaddle2)
        {
            petUser.PetData.Saddle = 10;
            UpdateHorsePetAndConsumeItem(petUser.PetData.PetId, item.Id, room, session, item, "have_saddle", "10");
        }
        else if (item.Definition.IsHorseHairstyle)
        {
            var parse = 100 + int.Parse(item.Definition.ItemName.Split('_')[2]);
            petUser.PetData.PetHair = parse;
            UpdateHorsePetAndConsumeItem(petUser.PetData.PetId, item.Id, room, session, item, "pethair", petUser.PetData.PetHair.ToString());
        }
        else if (item.Definition.IsHorseHairDye)
        {
            var hairDye = 48 + int.Parse(item.Definition.ItemName.Split('_')[2]);
            petUser.PetData.HairDye = hairDye;
            UpdateHorsePetAndConsumeItem(petUser.PetData.PetId, item.Id, room, session, item, "hairdye", petUser.PetData.HairDye.ToString());
        }
        else if (item.Definition.IsHorseBodyDye)
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

        room.SendPacket(new UsersComposer(petUser, _groupManager, room.GetCacheManager()));
        room.SendPacket(new PetHorseFigureInformationComposer(petUser));
        return Task.CompletedTask;
    }

    public Task RemoveSaddleFromHorse(GameClient session, int petId)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.TryGetCurrentRoom(out var currentRoom) || !_roomManager.TryGetRoom(currentRoom.Id, out var room) || room == null)
            return Task.CompletedTask;
        if (!room.GetRoomUserManager().TryGetPet(petId, out var petUser) || petUser.PetData == null || petUser.PetData.OwnerId != habbo.Id)
            return Task.CompletedTask;

        var saddleId = ItemUtility.GetSaddleId(petUser.PetData.Saddle);
        petUser.PetData.Saddle = 0;
        using (var connection = _database.Connection())
            connection.Execute(
                "UPDATE `bots_petdata` SET `have_saddle` = 0 WHERE `id` = @petId LIMIT 1",
                new { petId = petUser.PetData.PetId });

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

        room.SendPacket(new UsersComposer(petUser, _groupManager, room.GetCacheManager()));
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
            session.SendNotification(_languageManager.Require("bot.place.invalid_tile"));
            return Task.CompletedTask;
        }
        if (inventory?.Bots == null || !inventory.Bots.Bots.TryGetValue(botId, out var bot))
            return Task.CompletedTask;

        var botCount = room.GetRoomUserManager().GetUserList().Count(user => user != null && !user.IsPet && user.IsBot);
        if (botCount >= 5 && !(habbo.Permissions?.HasRight("bot_place_any_override") ?? false))
        {
            session.SendNotification(_languageManager.Require("bot.place.limit_reached"));
            return Task.CompletedTask;
        }

        using (var connection = _database.Connection())
            connection.Execute(
                "UPDATE `bots` SET `room_id` = @roomId, `x` = @coordX, `y` = @coordY WHERE `id` = @botId LIMIT 1",
                new { roomId = room.RoomId, botId = bot.Id, coordX = x, coordY = y });

        var botSpeechList = new List<RandomSpeech>();
        BotPlacementRow? getData;
        using (var connection = _database.Connection())
        {
            getData = connection.QueryFirstOrDefault<BotPlacementRow>(
                """
                SELECT
                    `ai_type` AS AiType,
                    `rotation` AS Rotation,
                    `walk_mode` AS WalkMode,
                    `automatic_chat` AS AutomaticChat,
                    `speaking_interval` AS SpeakingInterval,
                    `mix_sentences` AS MixSentences,
                    `chat_bubble` AS ChatBubble
                FROM `bots`
                WHERE `id` = @botId
                LIMIT 1
                """,
                new { botId = bot.Id });
            foreach (var speech in connection.Query<BotSpeechRow>(
                         "SELECT `text` AS Text FROM `bots_speech` WHERE `bot_id` = @botId",
                         new { botId = bot.Id }))
            {
                botSpeechList.Add(new(speech.Text, bot.Id));
            }
        }

        if (getData == null)
            return Task.CompletedTask;

        var botUser = room.GetRoomUserManager().DeployBot(
            new(bot.Id, room.RoomId, getData.AiType, getData.WalkMode, bot.Name, "", bot.Figure, x, y, 0, 4, 0, 0, 0, 0,
                ref botSpeechList, "", 0, bot.OwnerId, ConvertExtensions.EnumToBool(getData.AutomaticChat), getData.SpeakingInterval,
                ConvertExtensions.EnumToBool(getData.MixSentences), getData.ChatBubble), null!);
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
        if (habbo == null || !habbo.TryGetCurrentRoom(out var room) || botId == 0)
            return Task.CompletedTask;
        if (!room.GetRoomUserManager().TryGetBot(botId, out var botUser) || botUser.BotData == null)
            return Task.CompletedTask;
        if (habbo.Id != botUser.BotData.OwnerId && !(habbo.Permissions?.HasRight("bot_place_any_override") ?? false))
        {
            session.SendWhisper(_languageManager.Require("bot.pickup.owner_only"));
            return Task.CompletedTask;
        }

        using (var connection = _database.Connection())
            connection.Execute(
                "UPDATE `bots` SET `room_id` = 0 WHERE `id` = @id LIMIT 1",
                new { id = botId });

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
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var room) || !room.GetRoomUserManager().TryGetBot(botId, out var botUser))
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
        if (habbo == null || !habbo.TryGetCurrentRoom(out var room) || actionId < 1 || actionId > 5)
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
                using (var connection = _database.Connection())
                    connection.Execute(
                        "UPDATE `bots` SET `look` = @look, `gender` = @gender WHERE `id` = @id LIMIT 1",
                        new { look = habbo.Look, gender = habbo.Gender, id = bot.BotData.Id });
                break;
            case 2:
                SaveBotSpeech(botId, roomBot, dataString);
                break;
            case 3:
                roomBot.WalkingMode = roomBot.WalkingMode == "stand" ? "freeroam" : "stand";
                using (var connection = _database.Connection())
                    connection.Execute(
                        "UPDATE `bots` SET `walk_mode` = @walkMode WHERE `id` = @id LIMIT 1",
                        new { walkMode = roomBot.WalkingMode, id = roomBot.Id });
                break;
            case 4:
                roomBot.DanceId = roomBot.DanceId > 0 ? 0 : Random.Shared.Next(1, 4);
                room.SendPacket(new DanceComposer(bot, roomBot.DanceId));
                break;
            case 5:
                if (dataString.Length == 0)
                {
                    session.SendWhisper(_languageManager.Require("bot.name.required"));
                    return Task.CompletedTask;
                }
                if (dataString.Length >= 16)
                {
                    session.SendWhisper(_languageManager.Require("bot.name.too_long"));
                    return Task.CompletedTask;
                }
                if (dataString.Contains("<img src") || dataString.Contains("<font ") || dataString.Contains("</font>") || dataString.Contains("</a>") || dataString.Contains("<i>"))
                {
                    session.SendWhisper(_languageManager.Require("bot.name.html_disallowed"));
                    return Task.CompletedTask;
                }
                roomBot.Name = dataString;
                using (var connection = _database.Connection())
                    connection.Execute(
                        "UPDATE `bots` SET `name` = @name WHERE `id` = @id LIMIT 1",
                        new { name = dataString, id = roomBot.Id });
                room.SendPacket(new UsersComposer(bot, _groupManager, room.GetCacheManager()));
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

        using var connection = _database.Connection();
        connection.Execute(
            "DELETE FROM `bots_speech` WHERE `bot_id` = @botId",
            new { botId = roomBot.Id });
        for (var i = 0; i <= speechData.Length - 1; i++)
        {
            speechData[i] = Regex.Replace(speechData[i], "<(.|\\n)*?>", string.Empty);
            connection.Execute(
                "INSERT INTO `bots_speech` (`bot_id`, `text`) VALUES (@id, @data)",
                new { id = botId, data = speechData[i] });
        }
        connection.Execute(
            """
            UPDATE `bots`
            SET `automatic_chat` = @automaticChat, `speaking_interval` = @speakingInterval, `mix_sentences` = @mixChat
            WHERE `id` = @id
            LIMIT 1
            """,
            new
            {
                id = botId,
                automaticChat = automaticChat.ToLower(),
                speakingInterval = Convert.ToInt32(speakingInterval),
                mixChat = ConvertExtensions.ToStringEnumValue(roomBot.MixSentences)
            });

        roomBot.RandomSpeech.Clear();
        foreach (var speech in connection.Query<BotSpeechRow>(
                     "SELECT `text` AS Text FROM `bots_speech` WHERE `bot_id` = @id",
                     new { id = botId }))
        {
            roomBot.RandomSpeech.Add(new(speech.Text, botId));
        }
    }

    private void UpdateHorsePetAndConsumeItem(int petId, uint itemId, Room room, GameClient session, Item item, string field, string value)
    {
        using (var connection = _database.Connection())
        {
            connection.Execute(
                $"UPDATE `bots_petdata` SET `{field}` = @value WHERE `id` = @petId LIMIT 1",
                new { value, petId });
            connection.Execute(
                "DELETE FROM `items` WHERE `id` = @id LIMIT 1",
                new { id = item.Id });
        }
        room.GetRoomItemHandler().RemoveFurniture(session, itemId);
    }
}
