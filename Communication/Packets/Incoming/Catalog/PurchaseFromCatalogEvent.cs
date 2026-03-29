using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;
using Microsoft.Extensions.Logging;

namespace Plus.Communication.Packets.Incoming.Catalog;

public class PurchaseFromCatalogEvent : IPacketEvent
{
    private readonly ICatalogService _catalogService;
    private readonly ILogger<PurchaseFromCatalogEvent> _logger;

    public PurchaseFromCatalogEvent(ICatalogService catalogService, ILogger<PurchaseFromCatalogEvent> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var pageId = packet.ReadInt();
        var itemId = packet.ReadInt();
        var extraData = packet.ReadString();
        var amount = packet.ReadInt();
        _logger.LogInformation("PurchaseFromCatalogEvent received for session {sessionId}. PageId: {pageId}. ItemId: {itemId}. Amount: {amount}.", session.Id, pageId, itemId, amount);

        await _catalogService.PurchaseItem(session, pageId, itemId, extraData, amount);
    }
}
