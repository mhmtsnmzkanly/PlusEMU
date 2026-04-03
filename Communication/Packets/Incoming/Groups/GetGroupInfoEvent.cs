using Plus.Communication.Packets.Outgoing.Groups;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class GetGroupInfoEvent : IPacketEvent
{
    private readonly IGroupManager _groupManager;
    private readonly IRoomFactory _roomFactory;
    private readonly ICacheManager _cacheManager;

    public GetGroupInfoEvent(IGroupManager groupManager, IRoomFactory roomFactory, ICacheManager cacheManager)
    {
        _groupManager = groupManager;
        _roomFactory = roomFactory;
        _cacheManager = cacheManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var groupId = packet.ReadInt();
        var newWindow = packet.ReadBool();
        if (!_groupManager.TryGetGroup(groupId, out var group) || group == null)
            return Task.CompletedTask;
        session.Send(new GroupInfoComposer(group, session, _roomFactory, _cacheManager, newWindow));
        return Task.CompletedTask;
    }
}
