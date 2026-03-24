using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class IgnoreUserEvent : IPacketEvent
{
    private readonly IAchievementManager _achievementManager;
    private readonly IGameClientManager _gameClientManager;

    public IgnoreUserEvent(IAchievementManager achievementManager, IGameClientManager gameClientManager)
    {
        _achievementManager = achievementManager;
        _gameClientManager = gameClientManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (!habbo.InRoom)
            return Task.CompletedTask;
        var room = habbo.CurrentRoom;
        if (room == null)
            return Task.CompletedTask;
        var username = packet.ReadString();
        var player = _gameClientManager.GetClientByUsername(username)?.GetHabbo();
        if (player == null || (player.Permissions?.HasRight("mod_tool") ?? false))
            return Task.CompletedTask;
        if (habbo.IgnoresComponent?.IsIgnored(player.Id) == true)
            return Task.CompletedTask;
        habbo.IgnoresComponent?.Ignore(player.Id);
        _achievementManager.ProgressAchievement(session, "ACH_SelfModIgnoreSeen", 1);
        return Task.CompletedTask;
    }
}
