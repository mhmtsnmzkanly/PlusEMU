using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class BanUserEvent : IPacketEvent
{
    private readonly IAchievementService _achievementService;

    public BanUserEvent(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var room))
            return;

        if (room.WhoCanBan == 0 && !room.CheckRights(session, true) && room.Group == null || room.WhoCanBan == 1 && !room.CheckRights(session) && room.Group == null ||
            room.Group != null && !room.CheckRights(session, false, true))
            return;
        var userId = packet.ReadInt();
        packet.ReadInt(); //roomId
        var r = packet.ReadString();
        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(Convert.ToInt32(userId), out var user) || user == null || user.IsBot)
            return;
        if (room.OwnerId == userId)
            return;
        var targetClient = user.GetClient();
        if (targetClient?.GetHabbo() is not { } targetHabbo || (targetHabbo.Permissions?.HasRight("mod_tool") ?? false))
            return;
        long time = 0;
        if (r.ToLower().Contains("hour"))
            time = 3600;
        else if (r.ToLower().Contains("day"))
            time = 86400;
        else if (r.ToLower().Contains("perm"))
            time = 78892200;
        room.GetBans().Ban(user, time);
        await _achievementService.ProgressAchievement(session, "ACH_SelfModBanSeen", 1);
    }
}
