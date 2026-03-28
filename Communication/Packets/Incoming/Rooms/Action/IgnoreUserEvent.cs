using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class IgnoreUserEvent : IPacketEvent
{
    private readonly IAchievementService _achievementService;
    private readonly IGameClientManager _gameClientManager;

    public IgnoreUserEvent(IAchievementService achievementService, IGameClientManager gameClientManager)
    {
        _achievementService = achievementService;
        _gameClientManager = gameClientManager;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.InRoom != true)
            return;
        if (!habbo.TryGetCurrentRoom(out _))
            return;
        var username = packet.ReadString();
        var player = _gameClientManager.GetClientByUsername(username)?.GetHabbo();
        if (player == null || (player.Permissions?.HasRight("mod_tool") ?? false))
            return;
        if (habbo.IgnoresComponent?.IsIgnored(player.Id) == true)
            return;
        habbo.IgnoresComponent?.Ignore(player.Id);
        await _achievementService.ProgressAchievement(session, "ACH_SelfModIgnoreSeen", 1);
    }
}
