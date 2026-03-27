using Plus.Communication.Packets.Outgoing.Groups;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class GetGroupInfoEvent : IPacketEvent
{
    private readonly IGroupManager _groupManager;
    private readonly IRoomFactory _roomFactory;

    public GetGroupInfoEvent(IGroupManager groupManager, IRoomFactory roomFactory)
    {
        _groupManager = groupManager;
        _roomFactory = roomFactory;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var groupId = packet.ReadInt();
        var newWindow = packet.ReadBool();
        if (!_groupManager.TryGetGroup(groupId, out var group))
            return Task.CompletedTask;
        session.Send(new GroupInfoComposer(group, session, _roomFactory, newWindow));
        return Task.CompletedTask;
    }
}