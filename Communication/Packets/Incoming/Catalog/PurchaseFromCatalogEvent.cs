using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Catalog;

public class PurchaseFromCatalogEvent : IPacketEvent
{
    private readonly ICatalogService _catalogService;

    public PurchaseFromCatalogEvent(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var pageId = packet.ReadInt();
        var itemId = packet.ReadInt();
        var extraData = packet.ReadString();
        var amount = packet.ReadInt();

        await _catalogService.PurchaseItem(session, pageId, itemId, extraData, amount);
    }
}
