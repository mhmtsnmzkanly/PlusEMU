using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class MuteUserEvent : IPacketEvent
{
    private readonly IAchievementService _achievementService;

    public MuteUserEvent(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { Permissions: { } } habbo || !habbo.TryGetCurrentRoom(out var room))
            return;

        var userId = packet.ReadInt();
        packet.ReadInt(); //roomId
        var time = packet.ReadInt();
        if (room.WhoCanMute == 0 && !room.CheckRights(session, true) && room.Group == null || room.WhoCanMute == 1 && !room.CheckRights(session) && room.Group == null ||
            room.Group != null && !room.CheckRights(session, false, true))
            return;
        var target = room.GetRoomUserManager().GetRoomUserByHabbo(userId);
        var targetClient = target?.GetClient();
        if (targetClient?.GetHabbo() is not { } targetHabbo)
            return;
        if (targetHabbo.Permissions?.HasRight("mod_tool") == true)
            return;
        if (room.MutedUsers.ContainsKey(userId))
        {
            if (room.MutedUsers[userId] < UnixTimestamp.GetNow())
                room.MutedUsers.Remove(userId);
            else
                return;
        }
        room.MutedUsers.Add(userId, UnixTimestamp.GetNow() + time * 60);
        targetClient.SendWhisper($"The room owner has muted you for {time} minutes!");
        await _achievementService.ProgressAchievement(session, "ACH_SelfModMuteSeen", 1);
    }
}
