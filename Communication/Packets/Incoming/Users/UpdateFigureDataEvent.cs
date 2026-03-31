using Dapper;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Core.FigureData;
using Plus.Database;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Microsoft.Extensions.Logging;

namespace Plus.Communication.Packets.Incoming.Users;

internal class UpdateFigureDataEvent : IPacketEvent
{
    private readonly IFigureDataManager _figureManager;
    private readonly IAchievementService _achievementService;
    private readonly IQuestService _questService;
    private readonly IDatabase _database;
    private readonly ILogger<UpdateFigureDataEvent> _logger;

    public UpdateFigureDataEvent(IFigureDataManager figureDataManager, IAchievementService achievementService, IQuestService questService, IDatabase database, ILogger<UpdateFigureDataEvent> logger)
    {
        _figureManager = figureDataManager;
        _achievementService = achievementService;
        _questService = questService;
        _database = database;
        _logger = logger;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { Clothing: { } clothing } habbo)
            return;

        var gender = packet.ReadString().ToUpper();
        var requestedLook = packet.ReadString();
        var processedLook = _figureManager.ProcessFigure(requestedLook, gender, clothing.GetClothingParts, true);
        _logger.LogInformation("UpdateFigureDataEvent received for session {sessionId}. Gender: {gender}. RequestedLookLength: {lookLength}.", session.Id, gender, requestedLook.Length);

        if (processedLook == habbo.Look)
            return;
        if ((DateTime.Now - habbo.LastClothingUpdateTime).TotalSeconds <= 2.0)
        {
            habbo.ClothingUpdateWarnings += 1;
            if (habbo.ClothingUpdateWarnings >= 25)
                habbo.SessionClothingBlocked = true;

            return;
        }
        if (habbo.SessionClothingBlocked)
            return;

        habbo.LastClothingUpdateTime = DateTime.Now;
        string[] allowedGenders = { "M", "F" };
        if (!allowedGenders.Contains(gender))
        {
            session.Send(new BroadcastMessageAlertComposer("Sorry, you chose an invalid gender."));
            return;
        }

        await _questService.ProgressUserQuest(session, QuestType.ProfileChangeLook);
        habbo.Look = _figureManager.FilterFigure(processedLook);
        habbo.Gender = gender.ToLower();
        using var db = _database.Connection();
        db.Execute("UPDATE `users` SET `look` = @look, `gender` = @gender WHERE `id` = @id LIMIT 1",
            new { look = processedLook, gender, id = habbo.Id });
        await _achievementService.ProgressAchievement(session, "ACH_AvatarLooks", 1);
        session.Send(new AvatarAspectUpdateComposer(processedLook, gender));
        if (habbo.Look.Contains("ha-1006"))
            await _questService.ProgressUserQuest(session, QuestType.WearHat);
        if (habbo.TryGetCurrentRoom(out var currentRoom))
        {
            var roomUser = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
            if (roomUser != null)
            {
                session.Send(new UserChangeComposer(roomUser, true));
                currentRoom.SendPacket(new UserChangeComposer(roomUser, false));
            }
        }
    }
}
