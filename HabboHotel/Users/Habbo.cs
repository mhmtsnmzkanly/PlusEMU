using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users.Clothing;
using Plus.HabboHotel.Users.Effects;
using Plus.HabboHotel.Users.Ignores;
using Plus.HabboHotel.Users.Inventory;
using Plus.HabboHotel.Users.Messenger;
using Plus.HabboHotel.Users.Navigator;
using Plus.HabboHotel.Users.Permissions;
using Plus.HabboHotel.Users.UserData;
using Plus.Core.Settings;
using Plus.Database;
using Dapper;
using Plus.Utilities;

namespace Plus.HabboHotel.Users;

public class Habbo
{
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
    public Room? CurrentRoom { get; set; }
    public NavigatorPreferences? NavigatorPreferences { get; set; }

    public ConcurrentDictionary<string, UserAchievement> Achievements = new();
    public ArrayList FavoriteRooms = new();
    public Dictionary<int, int> Quests = new();
    public List<uint> RatedRooms = new();

    public bool InRoom => CurrentRoom != null;
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

    public UserAchievement? GetAchievementData(string group) =>
        Achievements.TryGetValue(group, out var data) ? data : null;

    public int GetQuestProgress(int questId) =>
        Quests.TryGetValue(questId, out var progress) ? progress : 0;

    public Plus.HabboHotel.Users.Messenger.FriendBar.FriendBarState FriendbarState
    {
        get => Plus.HabboHotel.Users.Messenger.FriendBar.FriendBarStateUtility.GetEnum(FriendBarState);
        set => FriendBarState = Plus.HabboHotel.Users.Messenger.FriendBar.FriendBarStateUtility.GetInt(value);
    }
    
    public void CheckCreditsTimer(Plus.Core.Settings.ISettingsManager settingsManager, Plus.HabboHotel.Subscriptions.ISubscriptionManager subscriptionManager) { } // placeholder - handled by process component

    public bool CacheExpired(double sessionStart) => (UnixTimestamp.GetNow() - sessionStart) > 300;

    public void Dispose() { }

    public void InitProcess(IDatabase database, Core.Settings.ISettingsManager settingsManager, object subscriptionManager, object achievementService)
    {
        // Handled by specialized process components
    }

    public void AttachClient(GameClient session)
    {
        Client = session;
        SessionStart = UnixTimestamp.GetNow();
    }

    public void DetachClient()
    {
        Client = null;
    }

    public bool TryGetClient(out GameClient client)
    {
        client = Client!;
        return client != null;
    }

    public void EnterRoom(Room room)
    {
        CurrentRoom = room;
    }

    public void LeaveRoom()
    {
        if (TentId > 0)
            TentId = 0;

        CurrentRoom = null;
    }

    public bool IsInRoom(Room room) => CurrentRoom == room;

    public bool TryGetCurrentRoom(out Room room)
    {
        room = CurrentRoom!;
        return room != null;
    }

    public void OnDisconnect()
    {
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public string GetQueryString
    {
        get
        {
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
