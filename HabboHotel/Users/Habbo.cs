using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Subscriptions;
using Plus.HabboHotel.Users.Clothing;
using Plus.HabboHotel.Users.Effects;
using Plus.HabboHotel.Users.Ignores;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Messenger;
using Plus.HabboHotel.Users.Navigator;
using Plus.HabboHotel.Users.Permissions;
using Plus.HabboHotel.Users.Process;
using Plus.HabboHotel.Users.UserData;
using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Core.Settings;
using Plus.Database;
using Dapper;
using NLog;
using Plus.Utilities;

namespace Plus.HabboHotel.Users;

public class Habbo
{
    private static readonly ILogger Log = LogManager.GetLogger("Plus.HabboHotel.Users.Habbo");
    private Room? _currentRoom;
    private bool _disconnected;
    private bool _habboSaved;
    private ProcessComponent? _process;
    private IDatabase? _database;

    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Rank { get; set; }
    public string Motto { get; set; } = string.Empty;
    public string Look { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public double LastOnline { get; set; }
    public int Credits { get; set; }
    public int Duckets { get; set; }
    public int Diamonds { get; set; }
    public int GotwPoints { get; set; }
    public uint HomeRoom { get; set; }
    public bool AllowFriendRequests { get; set; }
    public bool AppearOffline { get; set; }
    public bool AllowPublicRoomStatus { get; set; }
    public double AccountCreated { get; set; }
    public bool Vip { get; set; }
    public int VipRank { get; set; }
    public bool IsAmbassador { get; set; }
    public int CustomBubbleId { get; set; }
    public int AchievementPoints { get; set; }
    public int FavouriteGroupId { get; set; }
    public double LastNameChange { get; set; }
    public bool ChatPreference { get; set; }
    public bool FocusPreference { get; set; }
    public bool AllowPetSpeech { get; set; }
    public bool AllowBotSpeech { get; set; }
    public bool AllowMessengerInvites { get; set; }
    public bool AllowGifts { get; set; }
    public bool AllowMimic { get; set; }
    public int FriendBarState { get; set; }
    public bool DisableForcedEffects { get; set; }
    public double TimeMuted { get; set; }
    public bool AdvertisingReportBlocked { get; set; }
    public string MachineId { get; set; } = string.Empty;

    public bool WiredInteraction { get; set; }
    public bool AdvertisingReported { get; set; }
    public bool AdvertisingReportedBlocked { get; set; }

    // Preference flags
    public bool ReceiveWhispers { get; set; } = true;
    public bool IgnorePublicWhispers { get; set; }
    public bool AllowConsoleMessages { get; set; } = true;
    public bool AllowUserFollowing { get; set; } = true;
    public bool AllowTradingRequests { get; set; } = true;
    public int[] ClientVolume { get; set; } = [100, 100, 100];

    public HabboStats HabboStats { get; set; } = null!;
    public HabboMessenger? Messenger { get; set; }
    public InventoryComponent? Inventory { get; set; }
    public EffectsComponent? Effects { get; set; }
    public ClothingComponent? Clothing { get; set; }
    public IgnoresComponent? IgnoresComponent { get; set; }
    public PermissionComponent? Permissions { get; set; }
    public GameClient? Client { get; set; }
    public NavigatorPreferences? NavigatorPreferences { get; set; }
    public ConcurrentDictionary<int, Catalog.TargetedOfferPurchaseState> TargetedOfferPurchases { get; set; } = new();

    public ConcurrentDictionary<string, UserAchievement> Achievements = new();
    public ArrayList FavoriteRooms = new();
    public Dictionary<int, int> Quests = new();
    public List<uint> RatedRooms = new();

    public double FloodTime { get; set; }
    public int MessengerSpamCount { get; set; }
    public double MessengerSpamTime { get; set; }
    public int TimeAfk { get; set; }
    public bool ChangingName { get; set; }
    public int BannedPhraseCount { get; set; }
    public bool RoomAuthOk { get; set; }
    public int QuestLastCompleted { get; set; }
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
    public int FastfoodScore { get; set; }
    public int PetId { get; set; }
    public int CreditsUpdateTick { get; set; }
    public Rooms.Chat.Commands.ICommandBase? ChatCommand { get; set; }

    public DateTime LastGiftPurchaseTime { get; set; }
    public DateTime LastMottoUpdateTime { get; set; }
    public DateTime LastClothingUpdateTime { get; set; }
    public int GiftPurchasingWarnings { get; set; }
    public int MottoUpdateWarnings { get; set; }
    public int ClothingUpdateWarnings { get; set; }
    public bool SessionGiftBlocked { get; set; }
    public bool SessionMottoBlocked { get; set; }
    public bool SessionClothingBlocked { get; set; }

    public Calendar.CalendarComponent? Calendar { get; set; }

    public event EventHandler? Disconnected;

    public bool HasActiveClient => Client != null;
    internal bool HasProcessComponent => _process != null;

    public UserAchievement? GetAchievementData(string group) =>
        Achievements.TryGetValue(group, out var data) ? data : null;

    public int GetQuestProgress(int questId) =>
        Quests.TryGetValue(questId, out var progress) ? progress : 0;

    public Plus.HabboHotel.Users.Messenger.FriendBar.FriendBarState FriendbarState
    {
        get => Plus.HabboHotel.Users.Messenger.FriendBar.FriendBarStateUtility.GetEnum(FriendBarState);
        set => FriendBarState = Plus.HabboHotel.Users.Messenger.FriendBar.FriendBarStateUtility.GetInt(value);
    }
    
    public void CheckCreditsTimer(ISettingsManager settingsManager, ISubscriptionManager subscriptionManager)
    {
        try
        {
            CreditsUpdateTick--;
            if (CreditsUpdateTick > 0 || !TryGetClient(out var client))
                return;

            var creditUpdate = settingsManager.GetIntOrDefault("user.currency_scheduler.credit_reward", 0);
            var ducketUpdate = settingsManager.GetIntOrDefault("user.currency_scheduler.ducket_reward", 0);
            if (subscriptionManager.TryGetSubscriptionData(VipRank, out var subscriptionData) && subscriptionData != null)
            {
                creditUpdate += subscriptionData.Credits;
                ducketUpdate += subscriptionData.Duckets;
            }

            Credits += creditUpdate;
            Duckets += ducketUpdate;
            client.Send(new CreditBalanceComposer(Credits));
            client.Send(new HabboActivityPointNotificationComposer(Duckets, ducketUpdate));
            CreditsUpdateTick = settingsManager.GetIntOrDefault("user.currency_scheduler.tick", 60);
        }
        catch
        {
        }
    }

    public bool CacheExpired(double sessionStart) => (UnixTimestamp.GetNow() - sessionStart) > 300;

    public void Dispose()
    {
        Effects?.Dispose();
        Clothing?.Dispose();
        Permissions?.Dispose();
    }

    internal void AttachProcess(ProcessComponent process, IDatabase database)
    {
        if (_process != null)
            return;

        _database = database;
        _process = process;
    }

    public void AttachClient(GameClient session)
    {
        Client = session;
        SessionStart = UnixTimestamp.GetNow();
        _disconnected = false;
        Log.Debug("Habbo attached to session. UserId={userId}, Username={username}, SessionId={sessionId}", Id, Username, session.Id);
    }

    public void DetachClient()
    {
        Log.Debug("Habbo detached from session. UserId={userId}, Username={username}, HadClient={hadClient}", Id, Username, Client != null);
        Client = null;
    }

    public bool TryGetClient(out GameClient client)
    {
        if (Client != null)
        {
            client = Client;
            return true;
        }

        client = null!;
        return false;
    }

    public void EnterRoom(Room room)
    {
        _currentRoom = room;
        Log.Debug("Habbo entered room reference. UserId={userId}, Username={username}, RoomId={roomId}", Id, Username, room.Id);
    }

    public void LeaveRoom()
    {
        if (TentId > 0)
            TentId = 0;

        if (_currentRoom != null)
            Log.Debug("Habbo cleared room reference. UserId={userId}, Username={username}, RoomId={roomId}", Id, Username, _currentRoom.Id);
        _currentRoom = null;
    }

    public bool IsInRoom(Room room) => _currentRoom == room;

    public bool TryGetCurrentRoom(out Room room)
    {
        if (_currentRoom != null)
        {
            room = _currentRoom;
            return true;
        }

        room = null!;
        return false;
    }

    public void OnDisconnect()
    {
        if (_disconnected)
            return;

        Log.Info("Habbo disconnect start. UserId={userId}, Username={username}, InRoom={inRoom}, HasClient={hasClient}", Id, Username, _currentRoom != null, Client != null);
        _disconnected = true;
        Disconnected?.Invoke(this, EventArgs.Empty);
        try
        {
            _process?.Dispose();
        }
        catch
        {
        }
        try
        {
            if (TryGetClient(out var client) && TryGetCurrentRoom(out var room))
            {
                if (room.IsDisposed || room.Unloaded)
                    LeaveRoom();
                else
                    room.GetRoomService().HandleDisconnect(client).GetAwaiter().GetResult();
            }
            else
            {
                LeaveRoom();
            }

            SaveStateOnDisconnect();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to persist habbo state during disconnect for user {userId}.", Id);
        }
        finally
        {
            Log.Debug("Habbo disconnect cleanup running. UserId={userId}, Username={username}", Id, Username);
            Dispose();
            Log.Info("Habbo disconnect completed. UserId={userId}, Username={username}", Id, Username);
        }
    }

    private void SaveStateOnDisconnect()
    {
        if (_habboSaved || _database == null)
            return;

        Log.Debug("Persisting habbo state on disconnect. UserId={userId}, Username={username}, Credits={credits}, Duckets={duckets}, Diamonds={diamonds}", Id, Username, Credits, Duckets, Diamonds);
        _habboSaved = true;
        using var connection = _database.Connection();
        connection.Execute(
            "UPDATE `users` SET `online` = '0', `last_online` = @lastOnline, `activity_points` = @duckets, `credits` = @credits, `vip_points` = @diamonds, `home_room` = @homeRoom, `gotw_points` = @gotwPoints, `time_muted` = @timeMuted, `friend_bar_state` = @friendBarState, `bubble_id` = @customBubbleId WHERE `id` = @id LIMIT 1;" +
            "UPDATE `user_statistics` SET `roomvisits` = @roomVisits, `onlineTime` = @onlineTime, `respect` = @respect, `respectGiven` = @respectGiven, `giftsGiven` = @giftsGiven, `giftsReceived` = @giftsReceived, `dailyRespectPoints` = @dailyRespectPoints, `dailyPetRespectPoints` = @dailyPetRespectPoints, `AchievementScore` = @achievementScore, `quest_id` = @questId, `quest_progress` = @questProgress, `groupid` = @favouriteGroupId, `forum_posts` = @forumPosts WHERE `id` = @id LIMIT 1;",
            new
            {
                lastOnline = UnixTimestamp.GetNow(),
                duckets = Duckets,
                credits = Credits,
                diamonds = Diamonds,
                homeRoom = HomeRoom,
                gotwPoints = GotwPoints,
                timeMuted = TimeMuted,
                friendBarState = FriendBarState.ToString(),
                customBubbleId = CustomBubbleId,
                id = Id,
                roomVisits = HabboStats.RoomVisits,
                onlineTime = (int)(UnixTimestamp.GetNow() - SessionStart + HabboStats.OnlineTime),
                respect = HabboStats.Respect,
                respectGiven = HabboStats.RespectGiven,
                giftsGiven = HabboStats.GiftsGiven,
                giftsReceived = HabboStats.GiftsReceived,
                dailyRespectPoints = HabboStats.DailyRespectPoints,
                dailyPetRespectPoints = HabboStats.DailyPetRespectPoints,
                achievementScore = HabboStats.AchievementPoints,
                questId = HabboStats.QuestId,
                questProgress = HabboStats.QuestProgress,
                favouriteGroupId = HabboStats.FavouriteGroupId,
                forumPosts = HabboStats.ForumPosts
            });

        if (Permissions?.HasRight("mod_tickets") == true)
            connection.Execute("UPDATE `moderation_tickets` SET `status` = 'open', `moderator_id` = '0' WHERE `status` = 'picked' AND `moderator_id` = @id", new { id = Id });
    }

    public string GetQueryString
    {
        get
        {
            _habboSaved = true;
            return $"UPDATE `users` SET `online` = '0', `last_online` = '{UnixTimestamp.GetNow()}', `activity_points` = '{Duckets}', `credits` = '{Credits}', `vip_points` = '{Diamonds}', `home_room` = '{HomeRoom}', `gotw_points` = '{GotwPoints}', `time_muted` = '{TimeMuted}', `friend_bar_state` = '{FriendBarState}' WHERE id = '{Id}' LIMIT 1; " +
                   $"UPDATE `user_statistics` SET `roomvisits` = '{HabboStats.RoomVisits}', `onlineTime` = '{(int)(UnixTimestamp.GetNow() - SessionStart + HabboStats.OnlineTime)}', `respect` = '{HabboStats.Respect}', `respectGiven` = '{HabboStats.RespectGiven}', `giftsGiven` = '{HabboStats.GiftsGiven}', `giftsReceived` = '{HabboStats.GiftsReceived}', `dailyRespectPoints` = '{HabboStats.DailyRespectPoints}', `dailyPetRespectPoints` = '{HabboStats.DailyPetRespectPoints}', `AchievementScore` = '{HabboStats.AchievementPoints}', `quest_id` = '{HabboStats.QuestId}', `quest_progress` = '{HabboStats.QuestProgress}', `groupid` = '{HabboStats.FavouriteGroupId}', `forum_posts` = '{HabboStats.ForumPosts}' WHERE `id` = '{Id}' LIMIT 1;";
        }
    }

    public void SaveChatBubble(IDatabase database, int customBubbleId)
    {
        CustomBubbleId = customBubbleId;
        using var connection = database.Connection();
        connection.Execute("UPDATE `users` SET `bubble_id` = @customBubbleId WHERE `id` = @id LIMIT 1", new { customBubbleId, id = Id });
    }

    public void ChangeName(IDatabase database, string username)
    {
        Username = username;
        LastNameChange = UnixTimestamp.GetNow();
        using var connection = database.Connection();
        connection.Execute("UPDATE `users` SET `username` = @username, `last_change` = @lastChange WHERE `id` = @id LIMIT 1", new { username, lastChange = LastNameChange, id = Id });
    }
}
