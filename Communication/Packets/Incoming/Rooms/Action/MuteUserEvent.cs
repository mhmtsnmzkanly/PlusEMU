using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class MuteUserEvent : IPacketEvent
{
    private readonly IAchievementManager _achievementManager;

    public MuteUserEvent(IAchievementManager achievementManager)
    {
        _achievementManager = achievementManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null || !habbo.InRoom)
            return Task.CompletedTask;
        var userId = packet.ReadInt();
        packet.ReadInt(); //roomId
        var time = packet.ReadInt();
        var room = habbo.CurrentRoom;
        if (room == null)
            return Task.CompletedTask;
        if (room.WhoCanMute == 0 && !room.CheckRights(session, true) && room.Group == null || room.WhoCanMute == 1 && !room.CheckRights(session) && room.Group == null ||
            room.Group != null && !room.CheckRights(session, false, true))
            return Task.CompletedTask;
        var target = room.GetRoomUserManager().GetRoomUserByHabbo(PlusEnvironment.GetUsernameById(userId));
        var targetClient = target?.GetClient();
        var targetHabbo = targetClient?.GetHabbo();
        if (targetHabbo == null || targetClient == null)
            return Task.CompletedTask;
        if (targetHabbo.Permissions?.HasRight("mod_tool") == true)
            return Task.CompletedTask;
        if (room.MutedUsers.ContainsKey(userId))
        {
            if (room.MutedUsers[userId] < UnixTimestamp.GetNow())
                room.MutedUsers.Remove(userId);
            else
                return Task.CompletedTask;
        }
        room.MutedUsers.Add(userId, UnixTimestamp.GetNow() + time * 60);
        targetClient.SendWhisper($"The room owner has muted you for {time} minutes!");
        _achievementManager.ProgressAchievement(session, "ACH_SelfModMuteSeen", 1);
        return Task.CompletedTask;
    }
}
