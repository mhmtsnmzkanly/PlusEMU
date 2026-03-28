using Plus.Communication.Packets.Incoming.Rooms;
using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.Communication.Packets.Outgoing.Users;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Users;

internal class RespectUserEvent : RoomPacketEvent
{
    private readonly IAchievementService _achievementService;
    private readonly IQuestService _questService;

    public RespectUserEvent(IAchievementService achievementService, IQuestService questService)
    {
        _achievementService = achievementService;
        _questService = questService;
    }

    public override async Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats is not { } habboStats || habboStats.DailyRespectPoints <= 0)
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(packet.ReadInt());
        var targetClient = user?.GetClient();
        var targetHabbo = targetClient?.GetHabbo();
        if (user == null || targetHabbo?.HabboStats == null || targetHabbo.Id == habbo.Id || user.IsBot)
            return;

        var thisUser = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (thisUser == null)
            return;

        await _questService.ProgressUserQuest(session, QuestType.SocialRespect);
        await _achievementService.ProgressAchievement(session, "ACH_RespectGiven", 1);
        if (targetClient is not null)
            await _achievementService.ProgressAchievement(targetClient, "ACH_RespectEarned", 1);

        habboStats.DailyRespectPoints -= 1;
        habboStats.RespectGiven += 1;
        targetHabbo.HabboStats.Respect += 1;
        if (room.RespectNotificationsEnabled)
            room.SendPacket(new RespectNotificationComposer(targetHabbo.Id, targetHabbo.HabboStats.Respect));

        room.SendPacket(new ActionComposer(thisUser.VirtualId, 7));
    }
}
