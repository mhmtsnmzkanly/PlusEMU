using Plus.Communication.Packets.Outgoing.Groups;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class DeclineGroupMembershipEvent : IPacketEvent
{
    private readonly IGroupManager _groupManager;

    public DeclineGroupMembershipEvent(IGroupManager groupManager)
    {
        _groupManager = groupManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var groupId = packet.ReadInt();
        var userId = packet.ReadInt();
        if (!_groupManager.TryGetGroup(groupId, out var group))
            return Task.CompletedTask;
        if (habbo.Id != group.CreatorId && !group.IsAdmin(habbo.Id))
            return Task.CompletedTask;
        if (!group.HasRequest(userId))
            return Task.CompletedTask;
        group.HandleRequest(userId, false);
        session.Send(new UnknownGroupComposer(group.Id, userId));
        return Task.CompletedTask;
    }
}
