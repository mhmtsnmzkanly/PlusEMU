using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class KickUserEvent : IPacketEvent
{
    private readonly IAchievementService _achievementService;
    private readonly IRoomService _roomService;

    public KickUserEvent(IAchievementService achievementService, IRoomService roomService)
    {
        _achievementService = achievementService;
        _roomService = roomService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var room))
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
        await _roomService.KickFromRoom(targetClient);
        await _achievementService.ProgressAchievement(session, "ACH_SelfModKickSeen", 1);
    }
}
