using Plus.Communication.Packets.Outgoing.Groups;
using Plus.Communication.Packets.Outgoing.Rooms.Permissions;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.Cache.Type;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class RemoveGroupMemberEvent : IPacketEvent
{
    private readonly IGroupService _groupService;

    public RemoveGroupMemberEvent(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
        => _groupService.RemoveMember(session, packet.ReadInt(), packet.ReadInt());
}
