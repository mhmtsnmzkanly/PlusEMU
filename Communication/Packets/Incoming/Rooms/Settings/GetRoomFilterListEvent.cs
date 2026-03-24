using Plus.Communication.Packets.Outgoing.Rooms.Settings;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Settings;

internal class GetRoomFilterListEvent : IPacketEvent
{
    private readonly IAchievementManager _achievementManager;

    public GetRoomFilterListEvent(IAchievementManager achievementManager)
    {
        _achievementManager = achievementManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.InRoom)
            return Task.CompletedTask;
        var instance = habbo.CurrentRoom;
        if (instance == null)
            return Task.CompletedTask;
        if (!instance.CheckRights(session))
            return Task.CompletedTask;
        session.Send(new GetRoomFilterListComposer(instance));
        _achievementManager.ProgressAchievement(session, "ACH_SelfModRoomFilterSeen", 1);
        return Task.CompletedTask;
    }
}
