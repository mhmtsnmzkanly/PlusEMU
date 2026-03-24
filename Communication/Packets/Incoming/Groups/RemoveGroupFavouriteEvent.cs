using Plus.Communication.Packets.Outgoing.Groups;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class RemoveGroupFavouriteEvent : IPacketEvent
{
    private readonly IGroupService _groupService;

    public RemoveGroupFavouriteEvent(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _groupService.RemoveFavourite(session);
}
