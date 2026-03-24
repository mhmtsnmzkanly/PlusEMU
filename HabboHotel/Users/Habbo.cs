using System.Collections;
using System.Collections.Concurrent;
using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat.Commands;
using Plus.HabboHotel.Subscriptions;
using Plus.HabboHotel.Users.Clothing;
using Plus.HabboHotel.Users.Effects;
using Plus.HabboHotel.Users.Ignores;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Messenger;
using Plus.HabboHotel.Users.Messenger.FriendBar;
using Plus.HabboHotel.Users.Permissions;
using Plus.HabboHotel.Users.Process;
using Plus.Utilities;

using Dapper;
using Plus.HabboHotel.Users.Navigator;

namespace Plus.HabboHotel.Users;

public class Habbo
{
    public HabboStats HabboStats { get; set; } = null!;

    private readonly DateTime _timeCached = DateTime.Now;

    public GameClient? Client { get; set; }
    public ClothingComponent? Clothing { get; set; }

    private bool _disconnected;
    public EffectsComponent? Effects { get; set; }

    private bool _habboSaved;

    public IgnoresComponent? IgnoresComponent { get; set; }
    public InventoryComponent? Inventory { get; set; }

    public HabboMessenger? Messenger { get; set; }

    public NavigatorPreferences? NavigatorPreferences { get; set; }
    public PermissionComponent? Permissions { get; set; }

    [Obsolete("Should be deleted /refactored to standalone service")]
    private ProcessComponent? Process { get; set; }

    public ConcurrentDictionary<string, UserAchievement> Achievements = new();
    public ArrayList FavoriteRooms = new();
    public Dictionary<int, int> Quests = new();

    public List<uint> RatedRooms = new();

    // TODO @80O: Convert to uint
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public int Rank { get; set; }

    public bool IsAmbassador { get; set; }

    public string Motto { get; set; } = string.Empty;

    public string Look { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public int Credits { get; set; }

    public int Duckets { get; set; }

    public int Diamonds { get; set; }

    public int GotwPoints { get; set; }

    public uint HomeRoom { get; set; }

    public double LastOnline { get; set; }

    public double AccountCreated { get; set; }

    public List<int> ClientVolume { get; set; } = new() { 0, 0, 0 };

    public double LastNameChange { get; set; }

    public string MachineId { get; set; } = string.Empty;

    public bool ChatPreference { get; set; }

    public bool FocusPreference { get; set; }

    public int VipRank { get; set; }

    public bool AllowTradingRequests { get; set; }

    public bool AllowUserFollowing { get; set; }

    public bool AllowMessengerInvites { get; set; }

    public bool AllowPetSpeech { get; set; }

    public bool AllowBotSpeech { get; set; }

    public bool AllowConsoleMessages { get; set; } = true;

    public bool AllowGifts { get; set; }

    public bool AllowMimic { get; set; }

    public bool ReceiveWhispers { get; set; }

    public bool IgnorePublicWhispers { get; set; }

    public FriendBarState FriendbarState { get; set; }

    public int TimeAfk { get; set; }

    public bool DisableForcedEffects { get; set; }

    public bool ChangingName { get; set; }

    public double FloodTime { get; set; }

    public int BannedPhraseCount { get; set; }

    public bool RoomAuthOk { get; set; }

    public int QuestLastCompleted { get; set; }

    public int MessengerSpamCount { get; set; }

    public double MessengerSpamTime { get; set; }

    public double TimeMuted { get; set; }

    public double TradingLockExpiry { get; set; }

    public double SessionStart { get; set; }

    public uint TentId { get; set; }

    public uint HopperId { get; set; }

    public bool IsHopping { get; set; }

    public uint TeleporterId { get; set; }

    public bool IsTeleporting { get; set; }

    public uint TeleportingRoomId { get; set; }

    public bool HasSpoken { get; set; }

    public double LastAdvertiseReport { get; set; }

    public bool AdvertisingReported { get; set; }

    public bool AdvertisingReportedBlocked { get; set; }

    public bool WiredInteraction { get; set; }

    public int CustomBubbleId { get; set; }

    public int FastfoodScore { get; set; }

    public int PetId { get; set; }

    public int CreditsUpdateTick { get; set; }

    public ICommandBase? ChatCommand { get; set; }

    public DateTime LastGiftPurchaseTime { get; set; }

    public DateTime LastMottoUpdateTime { get; set; }

    public DateTime LastClothingUpdateTime { get; set; }

    public int GiftPurchasingWarnings { get; set; }

    public int MottoUpdateWarnings { get; set; }

    public int ClothingUpdateWarnings { get; set; }

    public bool SessionGiftBlocked { get; set; }

    public bool SessionMottoBlocked { get; set; }

    public bool SessionClothingBlocked { get; set; }

    public bool InRoom => CurrentRoom != null;

    public Room? CurrentRoom { get; set; }

    public string GetQueryString
    {
        get
        {
            _habboSaved = true;
            return
                $"UPDATE `users` SET `online` = false, `last_online` = '{UnixTimestamp.GetNow()}', `activity_points` = '{Duckets}', `credits` = '{Credits}', `vip_points` = '{Diamonds}', `home_room` = '{HomeRoom}', `gotw_points` = '{GotwPoints}', `time_muted` = '{TimeMuted}',`friend_bar_state` = '{FriendBarStateUtility.GetInt(FriendbarState)}' WHERE id = '{Id}' LIMIT 1;UPDATE `user_statistics` SET `roomvisits` = '{HabboStats.RoomVisits}', `onlineTime` = '{(UnixTimestamp.GetNow() - SessionStart + HabboStats.OnlineTime)}', `respect` = '{HabboStats.Respect}', `respectGiven` = '{HabboStats.RespectGiven}', `giftsGiven` = '{HabboStats.GiftsGiven}', `giftsReceived` = '{HabboStats.GiftsReceived}', `dailyRespectPoints` = '{HabboStats.DailyRespectPoints}', `dailyPetRespectPoints` = '{HabboStats.DailyPetRespectPoints}', `AchievementScore` = '{HabboStats.AchievementPoints}', `quest_id` = '{HabboStats.QuestId}', `quest_progress` = '{HabboStats.QuestProgress}', `groupid` = '{HabboStats.FavouriteGroupId}',`forum_posts` = '{HabboStats.ForumPosts}' WHERE `id` = '{Id}' LIMIT 1;";
        }
    }

    public bool CacheExpired()
    {
        var span = DateTime.Now - _timeCached;
        return span.TotalMinutes >= 30;
    }

    public bool InitProcess()
    {
        Process = new();
        return Process.Init(this);
    }

    public bool InitFx()
    {
        Effects = new();
        return Effects.Init(this);
    }

    public bool InitClothing()
    {
        Clothing = new();
        return Clothing.Init(this);
    }

    [Obsolete("Each loading task should be moved to their own IUserDataLoadingTask")]
    public void Init(GameClient client)
    {
        // Move each of these loading tasks to their own IUserDataLoadingTask implementation.
        //foreach (var id in data.FavouritedRooms) FavoriteRooms.Add(id);
        Client = client;
        //Quests = data.Quests;
        _disconnected = false;
        InitFx();
        InitClothing();
    }


    public event EventHandler? Disconnected;
    public void OnDisconnect()
    {
        if (_disconnected)
            return;

        Disconnected?.Invoke(this, EventArgs.Empty);

        try
        {
            if (Process != null)
                Process.Dispose();
        }
        catch { }
        _disconnected = true;
        PlusEnvironment.Game.ClientManager.UnregisterClient(Id, Username);
        if (!_habboSaved)
        {
            _habboSaved = true;
            using var dbClient = PlusEnvironment.DatabaseManager.GetQueryReactor();
            dbClient.RunQuery(
                $"UPDATE `users` SET `online` = false, `last_online` = '{(int)UnixTimestamp.GetNow()}', `activity_points` = '{Duckets}', `credits` = '{Credits}', `vip_points` = '{Diamonds}', `home_room` = '{HomeRoom}', `gotw_points` = '{GotwPoints}', `time_muted` = '{TimeMuted}',`friend_bar_state` = '{FriendBarStateUtility.GetInt(FriendbarState)}', `bubble_id` = '{CustomBubbleId}' WHERE id = '{Id}' LIMIT 1;UPDATE `user_statistics` SET `roomvisits` = '{HabboStats.RoomVisits}', `onlineTime` = '{(int)(UnixTimestamp.GetNow() - SessionStart + HabboStats.OnlineTime)}', `respect` = '{HabboStats.Respect}', `respectGiven` = '{HabboStats.RespectGiven}', `giftsGiven` = '{HabboStats.GiftsGiven}', `giftsReceived` = '{HabboStats.GiftsReceived}', `dailyRespectPoints` = '{HabboStats.DailyRespectPoints}', `dailyPetRespectPoints` = '{HabboStats.DailyPetRespectPoints}', `AchievementScore` = '{HabboStats.AchievementPoints}', `quest_id` = '{HabboStats.QuestId}', `quest_progress` = '{HabboStats.QuestProgress}', `groupid` = '{HabboStats.FavouriteGroupId}',`forum_posts` = '{HabboStats.ForumPosts}' WHERE `id` = '{Id}' LIMIT 1;");
            if (Permissions?.HasRight("mod_tickets") == true)
                dbClient.RunQuery($"UPDATE `moderation_tickets` SET `status` = 'open', `moderator_id` = '0' WHERE `status` ='picked' AND `moderator_id` = '{Id}'");
        }
        Dispose();
        Client = null;
    }

    public void Dispose()
    {
        if (InRoom && CurrentRoom != null && Client != null)
            CurrentRoom.GetRoomUserManager().RemoveUserFromRoom(Client, false);
        if (Effects != null)
            Effects.Dispose();
        if (Clothing != null)
            Clothing.Dispose();
        if (Permissions != null)
            Permissions.Dispose();
    }

    public void CheckCreditsTimer()
    {
        try
        {
            var client = Client;
            if (client == null)
                return;

            CreditsUpdateTick--;
            if (CreditsUpdateTick <= 0)
            {
                var creditUpdate = Convert.ToInt32(PlusEnvironment.SettingsManager.TryGetValue("user.currency_scheduler.credit_reward"));
                var ducketUpdate = Convert.ToInt32(PlusEnvironment.SettingsManager.TryGetValue("user.currency_scheduler.ducket_reward"));
                SubscriptionData? subData = null;
                if (PlusEnvironment.Game.SubscriptionManager.TryGetSubscriptionData(VipRank, out subData))
                {
                    creditUpdate += subData.Credits;
                    ducketUpdate += subData.Duckets;
                }
                Credits += creditUpdate;
                Duckets += ducketUpdate;
                client.Send(new CreditBalanceComposer(Credits));
                client.Send(new HabboActivityPointNotificationComposer(Duckets, ducketUpdate));
                CreditsUpdateTick = Convert.ToInt32(PlusEnvironment.SettingsManager.TryGetValue("user.currency_scheduler.tick"));
            }
        }
        catch { }
    }


    public int GetQuestProgress(int p)
    {
        Quests.TryGetValue(p, out var progress);
        return progress;
    }

    public UserAchievement? GetAchievementData(string p)
    {
        Achievements.TryGetValue(p, out var achievement);
        return achievement;
    }

    public void ChangeName(string username)
    {
        LastNameChange = UnixTimestamp.GetNow();
        Username = username;
        SaveKey("username", username);
        SaveKey("last_change", LastNameChange.ToString());
    }

    public void SaveChatBubble(string customBubbleId) => SaveKey("bubble_id", customBubbleId);

    public void SaveKey(string key, string value)
    {
        using var dbClient = PlusEnvironment.DatabaseManager.GetQueryReactor();
        dbClient.SetQuery($"UPDATE `users` SET {key} = @value WHERE `id` = '{Id}' LIMIT 1;");
        dbClient.AddParameter("value", value);
        dbClient.RunQuery();
    }

    public void PrepareRoom(uint id, string password)
    {
        var client = Client;
        if (client == null || client.GetHabbo() == null)
            return;

        var clientHabbo = client.GetHabbo();
        if (clientHabbo.InRoom)
        {
            var oldRoom = clientHabbo.CurrentRoom;
            oldRoom?.GetRoomUserManager().RemoveUserFromRoom(client, false);
        }
        if (clientHabbo.IsTeleporting && clientHabbo.TeleportingRoomId != id)
        {
            client.Send(new CloseConnectionComposer());
            return;
        }
        if (!PlusEnvironment.Game.RoomManager.TryLoadRoom(id, out var room))
        {
            client.Send(new CloseConnectionComposer());
            return;
        }
        if (room.IsCrashed)
        {
            client.SendNotification("This room has crashed! :(");
            client.Send(new CloseConnectionComposer());
            return;
        }
        if (room.GetRoomUserManager().UserCount >= room.UsersMax && !(clientHabbo.Permissions?.HasRight("room_enter_full") ?? false) && clientHabbo.Id != room.OwnerId)
        {
            client.Send(new CantConnectComposer(1));
            client.Send(new CloseConnectionComposer());
            return;
        }
        if (!(Permissions?.HasRight("room_ban_override") ?? false) && room.GetBans().IsBanned(Id))
        {
            RoomAuthOk = false;
            clientHabbo.RoomAuthOk = false;
            client.Send(new CantConnectComposer(4));
            client.Send(new CloseConnectionComposer());
            return;
        }
        client.Send(new OpenConnectionComposer());
        if (!room.CheckRights(client, true, true) && !clientHabbo.IsTeleporting && !clientHabbo.IsHopping)
        {
            if (room.Access == RoomAccess.Doorbell && !(clientHabbo.Permissions?.HasRight("room_enter_locked") ?? false))
            {
                if (room.UserCount > 0)
                {
                    client.Send(new DoorbellComposer(""));
                    room.SendPacket(new DoorbellComposer(clientHabbo.Username), true);
                    return;
                }
                client.Send(new FlatAccessDeniedComposer(""));
                client.Send(new CloseConnectionComposer());
                return;
            }
            if (room.Access == RoomAccess.Password && !(clientHabbo.Permissions?.HasRight("room_enter_locked") ?? false))
            {
                if (password.ToLower() != room.Password.ToLower() || string.IsNullOrWhiteSpace(password))
                {
                    client.Send(new GenericErrorComposer(-100002));
                    client.Send(new CloseConnectionComposer());
                    return;
                }
            }
        }
        if (!EnterRoom(room))
            client.Send(new CloseConnectionComposer());
    }

    public bool EnterRoom(Room room)
    {
        var client = Client;
        if (room == null || client == null || client.GetHabbo() == null)
            return false;
        var clientHabbo = client.GetHabbo();
        clientHabbo.CurrentRoom = room;
        client.Send(new RoomReadyComposer(room.RoomId, room.ModelName));
        if (room.Wallpaper != "0.0")
            client.Send(new RoomPropertyComposer("wallpaper", room.Wallpaper));
        if (room.Floor != "0.0")
            client.Send(new RoomPropertyComposer("floor", room.Floor));
        client.Send(new RoomPropertyComposer("landscape", room.Landscape));
        client.Send(new RoomRatingComposer(room.Score, !(clientHabbo.RatedRooms.Contains(room.RoomId) || room.OwnerId == clientHabbo.Id)));
        using (var dbClient = PlusEnvironment.DatabaseManager.Connection())
        {
            dbClient.Execute("INSERT INTO user_roomvisits (user_id,room_id,entry_timestamp,exit_timestamp) VALUES (@userId, @roomId, @entryTimestamp, @exitTimestamp)",
                new
                {
                    userId = clientHabbo.Id,
                    roomId = clientHabbo.CurrentRoom.RoomId,
                    entryTimestamp = UnixTimestamp.GetNow(),
                    exitTimestamp = 0,
                });
        }

        if (room.OwnerId != Id)
        {
            clientHabbo.HabboStats.RoomVisits += 1;
            PlusEnvironment.Game.AchievementManager.ProgressAchievement(client, "ACH_RoomEntry", 1);
        }
        return true;
    }
}
