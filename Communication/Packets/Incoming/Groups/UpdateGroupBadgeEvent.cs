using Plus.Communication.Packets.Outgoing.Groups;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class UpdateGroupBadgeEvent : IPacketEvent
{
    private readonly IGroupService _groupService;

    public UpdateGroupBadgeEvent(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var groupId = packet.ReadInt();
        var count = packet.ReadInt();
        var parts = new List<(int baseId, int firstPart, int secondPart)>(count);
        for (var i = 0; i < count; i++)
            parts.Add((packet.ReadInt(), packet.ReadInt(), packet.ReadInt()));
        return _groupService.UpdateBadge(session, groupId, parts);
    }
}
