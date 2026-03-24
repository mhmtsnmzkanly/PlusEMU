using Plus.Communication.Packets.Outgoing.Inventory.Badges;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Inventory.Badges;

internal class GetBadgesEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var badges = habbo.Inventory?.Badges?.Badges ?? new Dictionary<string, Plus.HabboHotel.Users.Badges.Badge>();
        session.Send(new BadgesComposer(habbo.Id, badges));
        return Task.CompletedTask;
    }
}
