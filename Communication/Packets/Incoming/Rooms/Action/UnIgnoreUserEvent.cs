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
        if (session.GetHabbo() is not { InRoom: true, IgnoresComponent: { } ignoresComponent } habbo || !habbo.TryGetCurrentRoom(out _))
            return Task.CompletedTask;

        var username = packet.ReadString();
        var player = _gameClientManager.GetClientByUsername(username)?.GetHabbo();
        if (player == null)
            return Task.CompletedTask;

        ignoresComponent.Unignore(player.Id);
        return Task.CompletedTask;
    }
}
