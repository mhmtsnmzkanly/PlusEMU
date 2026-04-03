using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.Core.Language;
using Plus.Core.Settings;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms.Chat.Commands;
using Plus.HabboHotel.Rooms.Chat.Filter;
using Plus.HabboHotel.Rooms.Chat.Logs;
using Plus.HabboHotel.Rooms.Chat.Styles;
using Plus.Utilities;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat;

public class ChatService : IChatService
{
    private readonly IChatStyleManager _chatStyleManager;
    private readonly IChatlogManager _chatlogManager;
    private readonly IWordFilterManager _wordFilterManager;
    private readonly ICommandManager _commandManager;
    private readonly IModerationActionService _moderationActionService;
    private readonly ILanguageManager _languageManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IQuestService _questService;

    public ChatService(
        IChatStyleManager chatStyleManager,
        IChatlogManager chatlogManager,
        IWordFilterManager wordFilterManager,
        ICommandManager commandManager,
        IModerationActionService moderationActionService,
        ILanguageManager languageManager,
        ISettingsManager settingsManager,
        IQuestService questService)
    {
        _chatStyleManager = chatStyleManager;
        _chatlogManager = chatlogManager;
        _wordFilterManager = wordFilterManager;
        _commandManager = commandManager;
        _moderationActionService = moderationActionService;
        _languageManager = languageManager;
        _settingsManager = settingsManager;
        _questService = questService;
    }

    public async Task Chat(GameClient session, string message, int styleId)
    {
        await ProcessChat(session, message, styleId, false);
    }

    public async Task Shout(GameClient session, string message, int styleId)
    {
        await ProcessChat(session, message, styleId, true);
    }

    public async Task Whisper(GameClient session, string targetUser, string message, int styleId)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null || habbo.HabboStats == null)
            return;

        if (string.IsNullOrWhiteSpace(message))
            return;

        message = StringCharFilter.Escape(message);

        if (!habbo.TryGetCurrentRoom(out var room))
            return;

        if (!habbo.Permissions.HasRight("room_ignore_mute") && room.CheckMute(session))
        {
            session.SendWhisper(_languageManager.Require("chat.muted"));
            return;
        }

        if (UnixTimestamp.GetNow() < habbo.FloodTime && habbo.FloodTime != 0)
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return;

        var user2 = room.GetRoomUserManager().GetRoomUserByHabbo(targetUser);
        if (user2 == null)
            return;

        if (habbo.TimeMuted > 0)
        {
            session.Send(new MutedComposer(habbo.TimeMuted));
            return;
        }

        if (!habbo.Permissions.HasRight("word_filter_override"))
            message = _wordFilterManager.CheckMessage(message);

        if (!_chatStyleManager.TryGetStyle(styleId, out var style) ||
            style.RequiredRight.Length > 0 && !habbo.Permissions.HasRight(style.RequiredRight))
            styleId = 0;

        user.LastBubble = habbo.CustomBubbleId == 0 ? styleId : habbo.CustomBubbleId;

        if (!habbo.Permissions.HasRight("mod_tool"))
        {
            if (user.IncrementAndCheckFlood(out var muteTime))
            {
                session.Send(new FloodControlComposer(muteTime));
                return;
            }
        }

        var targetClient = user2.GetClient();
        var targetHabbo = targetClient?.GetHabbo();
        if (targetHabbo == null)
            return;

        if (!targetHabbo.ReceiveWhispers && !habbo.Permissions.HasRight("room_whisper_override"))
        {
            session.SendWhisper(_languageManager.Require("chat.whispers.disabled"));
            return;
        }

        _chatlogManager.StoreChatlog(new(habbo.Id, room.Id, $"<Whisper to {targetUser}>: {message}", UnixTimestamp.GetNow(), habbo, room));

        if (_wordFilterManager.CheckBannedWords(message))
        {
            if (await HandleBannedWords(session, message, "whisper", 0, user.LastBubble))
                return;
        }

        await _questService.ProgressUserQuest(session, QuestType.SocialChat);
        user.UnIdle();

        session.Send(new WhisperComposer(user.VirtualId, message, 0, user.LastBubble));
        if (!user2.IsBot && user2.UserId != user.UserId)
        {
            if (targetClient != null && (targetHabbo.IgnoresComponent == null || !targetHabbo.IgnoresComponent.IsIgnored(habbo.Id)))
                targetClient.Send(new WhisperComposer(user.VirtualId, message, 0, user.LastBubble));
        }

        NotifyModeratorsOfWhisper(room, user, user2, targetUser, message);
    }

    public void ApplyTypingStatus(GameClient session, bool isTyping)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!habbo.TryGetCurrentRoom(out var room))
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Username);
        if (user == null)
            return;

        room.SendPacket(new UserTypingComposer(user.VirtualId, isTyping));
    }

    private async Task ProcessChat(GameClient session, string message, int styleId, bool shout)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null || habbo.HabboStats == null)
            return;

        if (string.IsNullOrWhiteSpace(message))
            return;

        message = StringCharFilter.Escape(message);
        if (message.Length > 100)
            message = message.Substring(0, 100);

        if (!habbo.TryGetCurrentRoom(out var room))
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return;

        if (!_chatStyleManager.TryGetStyle(styleId, out var style) ||
            style.RequiredRight.Length > 0 && !habbo.Permissions.HasRight(style.RequiredRight))
            styleId = 0;

        user.UnIdle();

        if (UnixTimestamp.GetNow() < habbo.FloodTime && habbo.FloodTime != 0)
            return;

        if (habbo.TimeMuted > 0)
        {
            session.Send(new MutedComposer(habbo.TimeMuted));
            return;
        }

        if (!habbo.Permissions.HasRight("room_ignore_mute") && room.CheckMute(session))
        {
            session.SendWhisper(_languageManager.Require("chat.muted"));
            return;
        }

        user.LastBubble = habbo.CustomBubbleId == 0 ? styleId : habbo.CustomBubbleId;

        if (!habbo.Permissions.HasRight("mod_tool"))
        {
            if (user.IncrementAndCheckFlood(out var muteTime))
            {
                session.Send(new FloodControlComposer(muteTime));
                return;
            }
        }

        _chatlogManager.StoreChatlog(new(habbo.Id, room.Id, message, UnixTimestamp.GetNow(), habbo, room));

        if (message.StartsWith(":", StringComparison.CurrentCulture) && await _commandManager.Parse(session, message))
            return;

        if (_wordFilterManager.CheckBannedWords(message))
        {
            if (await HandleBannedWords(session, message, shout ? "shout" : "chat", shout ? 1 : 0, user.LastBubble))
                return;
        }

        if (!habbo.Permissions.HasRight("word_filter_override"))
            message = _wordFilterManager.CheckMessage(message);

        await _questService.ProgressUserQuest(session, QuestType.SocialChat);
        user.OnChat(user.LastBubble, message, shout);
    }

    private async Task<bool> HandleBannedWords(GameClient session, string message, string type, int shoutMode, int bubbleId)
    {
        var habbo = session.GetHabbo();
        if (habbo == null) return false;

        habbo.BannedPhraseCount++;
        if (habbo.BannedPhraseCount >= _settingsManager.GetIntOrDefault("room.chat.filter.banned_phrases.chances", 0))
        {
            await _moderationActionService.Ban("System", ModerationBanType.Username, habbo.Username, $"Spamming banned phrases in {type} ({message})",
                UnixTimestamp.GetNow() + 78892200);
            session.Disconnect($"Auto-ban for banned phrase spam in {type}: {message}");
            return true;
        }

        var user = habbo.TryGetCurrentRoom(out var room)
            ? room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id)
            : null;
        if (user != null)
        {
            if (type == "whisper")
                session.Send(new WhisperComposer(user.VirtualId, message, 0, bubbleId));
            else if (shoutMode == 1)
                session.Send(new ShoutComposer(user.VirtualId, message, 0, bubbleId));
            else
                session.Send(new ChatComposer(user.VirtualId, message, 0, bubbleId));
        }
        return true;
    }

    private void NotifyModeratorsOfWhisper(HabboHotel.Rooms.Room room, HabboHotel.Rooms.RoomUser user, HabboHotel.Rooms.RoomUser user2, string targetName, string message)
    {
        var toNotify = room.GetRoomUserManager().GetRoomUserByRank(2);
        if (toNotify.Count > 0)
        {
            foreach (var notifiable in toNotify)
            {
                if (notifiable != null && notifiable.HabboId != user2.HabboId && notifiable.HabboId != user.HabboId)
                {
                    var notifiableClient = notifiable.GetClient();
                    var notifiableHabbo = notifiableClient?.GetHabbo();
                    if (notifiableClient != null && notifiableHabbo != null && !notifiableHabbo.IgnorePublicWhispers)
                        notifiableClient.Send(new WhisperComposer(user.VirtualId, $"[Whisper to {targetName}] {message}", 0, user.LastBubble));
                }
            }
        }
    }
}
