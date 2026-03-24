using Plus.Communication.Packets.Outgoing.Rooms.Engine;
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
    private readonly IAchievementManager _achievementManager;
    private readonly IQuestManager _questManager;
    private readonly IDatabase _database;

    public ChangeMottoEvent(IWordFilterManager wordFilterManager, IAchievementManager achievementManager, IQuestManager questManager, IDatabase database)
    {
        _wordFilterManager = wordFilterManager;
        _achievementManager = achievementManager;
        _questManager = questManager;
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null)
            return Task.CompletedTask;

        if (habbo.TimeMuted > 0)
        {
            session.SendNotification("Oops, you're currently muted - you cannot change your motto.");
            return Task.CompletedTask;
        }
        if ((DateTime.Now - habbo.LastMottoUpdateTime).TotalSeconds <= 2.0)
        {
            habbo.MottoUpdateWarnings += 1;
            if (habbo.MottoUpdateWarnings >= 25)
                habbo.SessionMottoBlocked = true;
            return Task.CompletedTask;
        }
        if (habbo.SessionMottoBlocked)
            return Task.CompletedTask;
        habbo.LastMottoUpdateTime = DateTime.Now;
        var newMotto = StringCharFilter.Escape(packet.ReadString().Trim());
        if (newMotto.Length > 38)
            newMotto = newMotto.Substring(0, 38);
        if (newMotto == habbo.Motto)
            return Task.CompletedTask;
        if (!habbo.Permissions.HasRight("word_filter_override"))
            newMotto = _wordFilterManager.CheckMessage(newMotto);
        habbo.Motto = newMotto;
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery("UPDATE `users` SET `motto` = @motto WHERE `id` = @userId LIMIT 1");
            dbClient.AddParameter("userId", habbo.Id);
            dbClient.AddParameter("motto", newMotto);
            dbClient.RunQuery();
        }
        _questManager.ProgressUserQuest(session, QuestType.ProfileChangeMotto);
        _achievementManager.ProgressAchievement(session, "ACH_Motto", 1);
        if (habbo.InRoom)
        {
            var room = habbo.CurrentRoom;
            if (room == null)
                return Task.CompletedTask;
            var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
            if (user == null || user.GetClient() == null)
                return Task.CompletedTask;
            room.SendPacket(new UserChangeComposer(user, false));
        }
        return Task.CompletedTask;
    }
}
