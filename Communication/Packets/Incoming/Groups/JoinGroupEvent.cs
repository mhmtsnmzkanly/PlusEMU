using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Groups;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class JoinGroupEvent : IPacketEvent
{
    private readonly IGroupService _groupService;

    public JoinGroupEvent(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _groupService.JoinGroup(session, packet.ReadInt());
}
