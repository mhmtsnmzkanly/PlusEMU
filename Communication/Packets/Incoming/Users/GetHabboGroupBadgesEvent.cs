using Plus.Communication.Packets.Outgoing.Users;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Incoming.Users;

internal class GetHabboGroupBadgesEvent : IPacketEvent
{
    private readonly IGroupManager _groupManager;

    public GetHabboGroupBadgesEvent(IGroupManager groupManager) => _groupManager = groupManager;

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { InRoom: true } habbo || !habbo.TryGetCurrentRoom(out var room))
            return Task.CompletedTask;

        var badges = _groupManager.GetAllBadgesInRoom(room);
        if (badges != null)
        {
            room.SendPacket(new HabboGroupBadgesComposer(badges));
            session.Send(new HabboGroupBadgesComposer(badges));
        }
        return Task.CompletedTask;
    }
}
