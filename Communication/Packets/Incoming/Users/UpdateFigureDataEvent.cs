using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Core.FigureData;
using Plus.Database;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;

namespace Plus.Communication.Packets.Incoming.Users;

internal class UpdateFigureDataEvent : IPacketEvent
{
    private readonly IFigureDataManager _figureManager;
    private readonly IAchievementManager _achievementManager;
    private readonly IQuestManager _questManager;
    private readonly IDatabase _database;

    public UpdateFigureDataEvent(IFigureDataManager figureDataManager, IAchievementManager achievementManager, IQuestManager questManager, IDatabase database)
    {
        _figureManager = figureDataManager;
        _achievementManager = achievementManager;
        _questManager = questManager;
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var clothing = habbo?.Clothing;
        if (habbo == null || clothing == null)
            return Task.CompletedTask;

        var gender = packet.ReadString().ToUpper();
        var look = _figureManager.ProcessFigure(packet.ReadString(), gender, clothing.GetClothingParts, true);
        if (look == habbo.Look)
            return Task.CompletedTask;
        if ((DateTime.Now - habbo.LastClothingUpdateTime).TotalSeconds <= 2.0)
        {
            habbo.ClothingUpdateWarnings += 1;
            if (habbo.ClothingUpdateWarnings >= 25)
                habbo.SessionClothingBlocked = true;
            return Task.CompletedTask;
        }
        if (habbo.SessionClothingBlocked)
            return Task.CompletedTask;
        habbo.LastClothingUpdateTime = DateTime.Now;
        string[] allowedGenders = { "M", "F" };
        if (!allowedGenders.Contains(gender))
        {
            session.Send(new BroadcastMessageAlertComposer("Sorry, you chose an invalid gender."));
            return Task.CompletedTask;
        }
        _questManager.ProgressUserQuest(session, QuestType.ProfileChangeLook);
        habbo.Look = _figureManager.FilterFigure(look);
        habbo.Gender = gender.ToLower();
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery($"UPDATE `users` SET `look` = @look, `gender` = @gender WHERE `id` = '{habbo.Id}' LIMIT 1");
            dbClient.AddParameter("look", look);
            dbClient.AddParameter("gender", gender);
            dbClient.RunQuery();
        }
        _achievementManager.ProgressAchievement(session, "ACH_AvatarLooks", 1);
        session.Send(new AvatarAspectUpdateComposer(look, gender));
        if (habbo.Look.Contains("ha-1006"))
            _questManager.ProgressUserQuest(session, QuestType.WearHat);
        if (habbo.InRoom)
        {
            var currentRoom = habbo.CurrentRoom;
            if (currentRoom == null)
                return Task.CompletedTask;

            var roomUser = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
            if (roomUser != null)
            {
                session.Send(new UserChangeComposer(roomUser, true));
                currentRoom.SendPacket(new UserChangeComposer(roomUser, false));
            }
        }
        return Task.CompletedTask;
    }
}
