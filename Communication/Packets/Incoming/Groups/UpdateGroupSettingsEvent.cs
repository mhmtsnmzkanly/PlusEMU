using Plus.Communication.Packets.Outgoing.Groups;
using Plus.Communication.Packets.Outgoing.Rooms.Permissions;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class UpdateGroupSettingsEvent : IPacketEvent
{
    private readonly IGroupService _groupService;

    public UpdateGroupSettingsEvent(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
        => _groupService.UpdateSettings(session, packet.ReadInt(), packet.ReadInt(), packet.ReadInt());
}
