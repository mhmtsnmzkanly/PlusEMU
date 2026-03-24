using Plus.Communication.Packets.Outgoing.Groups;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class AcceptGroupMembershipEvent : IPacketEvent
{
    private readonly IGroupManager _groupManager;

    public AcceptGroupMembershipEvent(IGroupManager groupManager)
    {
        _groupManager = groupManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null)
            return Task.CompletedTask;

        var groupId = packet.ReadInt();
        var userId = packet.ReadInt();
        if (!_groupManager.TryGetGroup(groupId, out var group))
            return Task.CompletedTask;
        if (habbo.Id != group.CreatorId && !group.IsAdmin(habbo.Id) && !habbo.Permissions.HasRight("fuse_group_accept_any"))
            return Task.CompletedTask;
        if (!group.HasRequest(userId))
            return Task.CompletedTask;
        var targetHabbo = PlusEnvironment.GetHabboById(userId);
        if (targetHabbo == null)
        {
            session.SendNotification("Oops, an error occurred whilst finding this user.");
            return Task.CompletedTask;
        }
        group.HandleRequest(userId, true);
        session.Send(new GroupMemberUpdatedComposer(groupId, targetHabbo, 4));
        return Task.CompletedTask;
    }
}
