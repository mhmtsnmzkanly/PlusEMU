using Dapper;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Core.Language;
using Plus.Database;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms.Chat.Filter;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.Rooms.Avatar;

internal class ChangeMottoEvent : IPacketEvent
{
    private readonly IWordFilterManager _wordFilterManager;
    private readonly IAchievementService _achievementService;
    private readonly IQuestService _questService;
    private readonly IDatabase _database;
    private readonly ILanguageManager _languageManager;

    public ChangeMottoEvent(IWordFilterManager wordFilterManager, IAchievementService achievementService, IQuestService questService, IDatabase database, ILanguageManager languageManager)
    {
        _wordFilterManager = wordFilterManager;
        _achievementService = achievementService;
        _questService = questService;
        _database = database;
        _languageManager = languageManager;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null) return;
        if (habbo.TimeMuted > 0) { session.SendNotification(_languageManager.Require("user.motto_change.muted")); return; }
        if ((DateTime.Now - habbo.LastMottoUpdateTime).TotalSeconds <= 2.0) { habbo.MottoUpdateWarnings += 1; if (habbo.MottoUpdateWarnings >= 25) habbo.SessionMottoBlocked = true; return; }
        if (habbo.SessionMottoBlocked) return;
        habbo.LastMottoUpdateTime = DateTime.Now;
        var newMotto = StringCharFilter.Escape(packet.ReadString().Trim());
        if (newMotto.Length > 38) newMotto = newMotto.Substring(0, 38);
        if (newMotto == habbo.Motto) return;
        if (!habbo.Permissions.HasRight("word_filter_override")) newMotto = _wordFilterManager.CheckMessage(newMotto);
        habbo.Motto = newMotto;
        using var db = _database.Connection();
        db.Execute("UPDATE `users` SET `motto` = @motto WHERE `id` = @userId LIMIT 1", new { motto = newMotto, userId = habbo.Id });
        await _questService.ProgressUserQuest(session, QuestType.ProfileChangeMotto);
        await _achievementService.ProgressAchievement(session, "ACH_Motto", 1);
        if (habbo.TryGetCurrentRoom(out var room))
        {
            var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
            if (user == null || user.GetClient() == null) return;
            room.SendPacket(new UserChangeComposer(user, false));
        }
    }
}
