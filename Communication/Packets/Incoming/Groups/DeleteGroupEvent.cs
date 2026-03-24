using Plus.Core.Settings;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class DeleteGroupEvent : IPacketEvent
{
    private readonly IGroupService _groupService;

    public DeleteGroupEvent(IGroupService groupService)
    {
        _groupService = groupService;
    }
    public Task Parse(GameClient session, IIncomingPacket packet) => _groupService.DeleteGroup(session, packet.ReadInt());
}
