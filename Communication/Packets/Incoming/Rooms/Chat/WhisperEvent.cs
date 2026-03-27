using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.Rooms.Chat.Commands;
using Plus.Core.Settings;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms.Chat.Filter;
using Plus.HabboHotel.Rooms.Chat.Logs;
using Plus.HabboHotel.Rooms.Chat.Styles;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.Rooms.Chat;

public class WhisperEvent : IPacketEvent
{
    private readonly IChatStyleManager _chatStyleManager;
    private readonly IChatlogManager _chatlogManager;
    private readonly IWordFilterManager _wordFilterManager;
    private readonly ICommandManager _commandManager;
    private readonly IModerationManager _moderationManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IQuestService _questService;

    public WhisperEvent(
        IChatStyleManager chatStyleManager,
        IChatlogManager chatlogManager,
        IWordFilterManager wordFilterManager,
        ICommandManager commandManager,
        IModerationManager moderationManager,
        ISettingsManager settingsManager,
        IQuestService questService)
    {
        _chatStyleManager = chatStyleManager;
        _chatlogManager = chatlogManager;
        _wordFilterManager = wordFilterManager;
        _commandManager = commandManager;
        _moderationManager = moderationManager;
        _settingsManager = settingsManager;
        _questService = questService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null || habbo.HabboStats == null || !habbo.InRoom)
            return;
        var room = habbo.CurrentRoom;
        if (room == null)
            return;
        if (!habbo.Permissions.HasRight("room_ignore_mute") && room.CheckMute(session))
        {
            session.SendWhisper("Oops, you're currently muted.");
            return;
        }
        if (UnixTimestamp.GetNow() < habbo.FloodTime && habbo.FloodTime != 0)
            return;
        var @params = packet.ReadString();
        if (string.IsNullOrWhiteSpace(@params) || !@params.Contains(' '))
            return;
        var toUser = @params.Split(' ')[0];
        var message = @params.Substring(toUser.Length + 1);
        var colour = packet.ReadInt();
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return;
        var user2 = room.GetRoomUserManager().GetRoomUserByHabbo(toUser);
        if (user2 == null)
            return;
        if (habbo.TimeMuted > 0)
        {
            session.Send(new MutedComposer(habbo.TimeMuted));
            return;
        }
        if (!habbo.Permissions.HasRight("word_filter_override"))
            message = _wordFilterManager.CheckMessage(message);
        if (!_chatStyleManager.TryGetStyle(colour, out var style) ||
            style.RequiredRight.Length > 0 && !habbo.Permissions.HasRight(style.RequiredRight))
            colour = 0;
        user.LastBubble = habbo.CustomBubbleId == 0 ? colour : habbo.CustomBubbleId;
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
            session.SendWhisper("Oops, this user has their whispers disabled!");
            return;
        }
        _chatlogManager.StoreChatlog(new(habbo.Id, room.Id, $"<Whisper to {toUser}>: {message}", UnixTimestamp.GetNow(), habbo, room));
        if (_wordFilterManager.CheckBannedWords(message))
        {
            habbo.BannedPhraseCount++;
            if (habbo.BannedPhraseCount >= Convert.ToInt32(_settingsManager.TryGetValue("room.chat.filter.banned_phrases.chances")))
            {
                _moderationManager.BanUser("System", ModerationBanType.Username, habbo.Username, $"Spamming banned phrases ({message})",
                    UnixTimestamp.GetNow() + 78892200);
                session.Disconnect($"Auto-ban for banned phrase spam in whisper: {message}");
                return;
            }
            session.Send(new WhisperComposer(user.VirtualId, message, 0, user.LastBubble));
            return;
        }
        await _questService.ProgressUserQuest(session, QuestType.SocialChat);
        user.UnIdle();
        var userClient = user.GetClient();
        if (userClient == null)
            return;
        userClient.Send(new WhisperComposer(user.VirtualId, message, 0, user.LastBubble));
        if (!user2.IsBot && user2.UserId != user.UserId)
        {
            if (targetClient != null && (targetHabbo.IgnoresComponent == null || !targetHabbo.IgnoresComponent.IsIgnored(habbo.Id)))
                targetClient.Send(new WhisperComposer(user.VirtualId, message, 0, user.LastBubble));
        }
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
                        notifiableClient.Send(new WhisperComposer(user.VirtualId, $"[Whisper to {toUser}] {message}", 0, user.LastBubble));
                }
            }
        }
    }
}
