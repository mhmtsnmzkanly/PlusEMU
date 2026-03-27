using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class KickUserEvent : IPacketEvent
{
    private readonly IAchievementService _achievementService;

    public KickUserEvent(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var room = habbo?.CurrentRoom;
        if (room == null)
            return;
        if (!room.CheckRights(session) && room.WhoCanKick != 2 && room.Group == null)
            return;
        if (room.Group != null && !room.CheckRights(session, false, true))
            return;
        var userId = packet.ReadInt();
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(userId);
        if (user == null || user.IsBot)
            return;

        //Cannot kick owner or moderators.
        var targetClient = user.GetClient();
        var targetHabbo = targetClient?.GetHabbo();
        if (targetClient == null || targetHabbo == null)
            return;
        if (room.CheckRights(targetClient, true) || (targetHabbo.Permissions?.HasRight("mod_tool") ?? false))
            return;
        room.GetRoomUserManager().RemoveUserFromRoom(targetClient, true, true);
        await _achievementService.ProgressAchievement(session, "ACH_SelfModKickSeen", 1);
    }
}
