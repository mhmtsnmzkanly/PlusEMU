using Dapper;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Catalog;

namespace Plus.Communication.Packets.Incoming.Catalog;

public class RedeemVoucherEvent : IPacketEvent
{
    private readonly ICatalogService _catalogService;

    public RedeemVoucherEvent(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var code = packet.ReadString();
        if (string.IsNullOrWhiteSpace(code))
            return;

        await _catalogService.RedeemVoucher(session, code);
    }
}
