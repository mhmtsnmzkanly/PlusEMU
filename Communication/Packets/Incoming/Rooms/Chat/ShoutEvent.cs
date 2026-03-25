using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.Core.Settings;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms.Chat.Commands;
using Plus.HabboHotel.Rooms.Chat.Filter;
using Plus.HabboHotel.Rooms.Chat.Logs;
using Plus.HabboHotel.Rooms.Chat.Styles;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.Rooms.Chat;

public class ShoutEvent : IPacketEvent
{
    private readonly IChatStyleManager _chatStyleManager;
    private readonly IChatlogManager _chatlogManager;
    private readonly IWordFilterManager _wordFilterManager;
    private readonly ICommandManager _commandManager;
    private readonly IModerationManager _moderationManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IQuestManager _questManager;

    public ShoutEvent(
        IChatStyleManager chatStyleManager,
        IChatlogManager chatlogManager,
        IWordFilterManager wordFilterManager,
        ICommandManager commandManager,
        IModerationManager moderationManager,
        ISettingsManager settingsManager,
        IQuestManager questManager)
    {
        _chatStyleManager = chatStyleManager;
        _chatlogManager = chatlogManager;
        _wordFilterManager = wordFilterManager;
        _commandManager = commandManager;
        _moderationManager = moderationManager;
        _settingsManager = settingsManager;
        _questManager = questManager;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null || habbo.HabboStats == null || !habbo.InRoom)
            return;
        var room = habbo.CurrentRoom;
        if (room == null)
            return;
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return;
        var message = StringCharFilter.Escape(packet.ReadString());
        if (message.Length > 100)
            message = message.Substring(0, 100);
        var colour = packet.ReadInt();
        if (!_chatStyleManager.TryGetStyle(colour, out var style) ||
            style.RequiredRight.Length > 0 && !habbo.Permissions.HasRight(style.RequiredRight))
            colour = 0;
        user.LastBubble = habbo.CustomBubbleId == 0 ? colour : habbo.CustomBubbleId;
        if (UnixTimestamp.GetNow() < habbo.FloodTime && habbo.FloodTime != 0)
            return;
        if (habbo.TimeMuted > 0)
        {
            session.Send(new MutedComposer(habbo.TimeMuted));
            return;
        }
        if (!habbo.Permissions.HasRight("room_ignore_mute") && room.CheckMute(session))
        {
            session.SendWhisper("Oops, you're currently muted.");
            return;
        }
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
            habbo.BannedPhraseCount++;
            if (habbo.BannedPhraseCount >= Convert.ToInt32(_settingsManager.TryGetValue("room.chat.filter.banned_phrases.chances")))
            {
                _moderationManager.BanUser("System", ModerationBanType.Username, habbo.Username, $"Spamming banned phrases ({message})",
                    UnixTimestamp.GetNow() + 78892200);
                session.Disconnect($"Auto-ban for banned phrase spam in shout: {message}");
                return;
            }
            session.Send(new ShoutComposer(user.VirtualId, message, 0, colour));
            return;
        }
        if (!habbo.Permissions.HasRight("word_filter_override"))
            message = _wordFilterManager.CheckMessage(message);
        _questManager.ProgressUserQuest(session, QuestType.SocialChat);
        user.UnIdle();
        user.OnChat(user.LastBubble, message, true);
        return;
    }
}
