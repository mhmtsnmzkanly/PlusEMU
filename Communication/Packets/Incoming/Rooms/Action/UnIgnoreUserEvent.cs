using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class UnIgnoreUserEvent : IPacketEvent
{
    private readonly IGameClientManager _gameClientManager;

    public UnIgnoreUserEvent(IGameClientManager gameClientManager)
    {
        _gameClientManager = gameClientManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.IgnoresComponent == null || !habbo.InRoom)
            return Task.CompletedTask;
        var room = habbo.CurrentRoom;
        if (room == null)
            return Task.CompletedTask;
        var username = packet.ReadString();
        var player = _gameClientManager.GetClientByUsername(username)?.GetHabbo();
        if (player == null)
            return Task.CompletedTask;
        habbo.IgnoresComponent.Unignore(player.Id);
        return Task.CompletedTask;
    }
}
