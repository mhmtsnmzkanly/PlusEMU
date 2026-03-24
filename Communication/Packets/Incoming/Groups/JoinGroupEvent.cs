using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Groups;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class JoinGroupEvent : IPacketEvent
{
    private readonly IGroupManager _groupManager;
    private readonly IGameClientManager _clientManager;

    public JoinGroupEvent(IGroupManager groupManager, IGameClientManager clientManager)
    {
        _groupManager = groupManager;
        _clientManager = clientManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        if (!_groupManager.TryGetGroup(packet.ReadInt(), out var group))
            return Task.CompletedTask;
        if (group.IsMember(habbo.Id) || group.IsAdmin(habbo.Id) || group.HasRequest(habbo.Id) && group.Type == GroupType.Private)
            return Task.CompletedTask;
        var groups = _groupManager.GetGroupsForUser(habbo.Id);
        if (groups.Count >= 1500)
        {
            session.Send(new BroadcastMessageAlertComposer("Oops, it appears that you've hit the group membership limit! You can only join upto 1,500 groups."));
            return Task.CompletedTask;
        }
        group.AddMember(habbo.Id);
        if (group.Type == GroupType.Locked)
        {
            var groupAdmins = (from client in _clientManager.GetClients.ToList()
                where client != null && client.GetHabbo() != null && @group.IsAdmin(client.GetHabbo().Id)
                select client).ToList();
            foreach (var client in groupAdmins) client.Send(new GroupMembershipRequestedComposer(group.Id, habbo, 3));
            session.Send(new GroupInfoComposer(group, session));
        }
        else
        {
            session.Send(new GroupFurniConfigComposer(_groupManager.GetGroupsForUser(habbo.Id)));
            session.Send(new GroupInfoComposer(group, session));
            var currentRoom = habbo.CurrentRoom;
            if (currentRoom != null)
                currentRoom.SendPacket(new RefreshFavouriteGroupComposer(habbo.Id));
            else
                session.Send(new RefreshFavouriteGroupComposer(habbo.Id));
        }
        return Task.CompletedTask;
    }
}
