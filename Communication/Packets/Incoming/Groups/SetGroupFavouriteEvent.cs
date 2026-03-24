using Plus.Communication.Packets.Outgoing.Groups;
using Plus.Communication.Packets.Outgoing.Users;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class SetGroupFavouriteEvent : IPacketEvent
{
    private readonly IGroupService _groupService;

    public SetGroupFavouriteEvent(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _groupService.SetFavourite(session, packet.ReadInt());
}
