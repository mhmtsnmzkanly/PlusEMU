using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Subscriptions;

namespace Plus.Communication.Packets.Incoming.Inventory.Purse;

internal class GetHabboClubWindowEvent : IPacketEvent
{
    private readonly IClubCenterService _clubCenterService;

    public GetHabboClubWindowEvent(IClubCenterService clubCenterService) => _clubCenterService = clubCenterService;

    public Task Parse(GameClient session, IIncomingPacket packet) =>
        _clubCenterService.SendClubCenterData(session, packet.HasDataRemaining() ? packet.ReadInt() : 0);
}
