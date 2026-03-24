using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class KickUserEvent : IPacketEvent
{
    private readonly IAchievementManager _achievementManager;

    public KickUserEvent(IAchievementManager achievementManager)
    {
        _achievementManager = achievementManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var room = habbo?.CurrentRoom;
        if (room == null)
            return Task.CompletedTask;
        if (!room.CheckRights(session) && room.WhoCanKick != 2 && room.Group == null)
            return Task.CompletedTask;
        if (room.Group != null && !room.CheckRights(session, false, true))
            return Task.CompletedTask;
        var userId = packet.ReadInt();
        var user = room.GetRoomUserManager().GetRoomUserByHabbo(userId);
        if (user == null || user.IsBot)
            return Task.CompletedTask;

        //Cannot kick owner or moderators.
        var targetClient = user.GetClient();
        var targetHabbo = targetClient?.GetHabbo();
        if (targetClient == null || targetHabbo == null)
            return Task.CompletedTask;
        if (room.CheckRights(targetClient, true) || (targetHabbo.Permissions?.HasRight("mod_tool") ?? false))
            return Task.CompletedTask;
        room.GetRoomUserManager().RemoveUserFromRoom(targetClient, true, true);
        _achievementManager.ProgressAchievement(session, "ACH_SelfModKickSeen", 1);
        return Task.CompletedTask;
    }
}
