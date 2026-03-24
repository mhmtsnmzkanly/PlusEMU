using Plus.Communication.Packets.Outgoing.Groups;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class UpdateGroupColoursEvent : IPacketEvent
{
    private readonly IGroupService _groupService;

    public UpdateGroupColoursEvent(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
        => _groupService.UpdateColours(session, packet.ReadInt(), packet.ReadInt(), packet.ReadInt());
}
