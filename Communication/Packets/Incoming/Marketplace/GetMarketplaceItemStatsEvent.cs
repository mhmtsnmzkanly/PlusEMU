using Dapper;
using Plus.Communication.Packets.Outgoing.Marketplace;
using Plus.Database;
using Plus.HabboHotel.Catalog.Marketplace;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Marketplace;

internal class GetMarketplaceItemStatsEvent : IPacketEvent
{
    private readonly IDatabase _database;
    private readonly IMarketplaceManager _marketplaceManager;

    public GetMarketplaceItemStatsEvent(IDatabase database, IMarketplaceManager marketplaceManager)
    {
        _database = database;
        _marketplaceManager = marketplaceManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var itemId = packet.ReadInt();
        var spriteId = packet.ReadUInt();
        using var db = _database.Connection();
        var avgPrice = db.QueryFirstOrDefault<int?>(
            "SELECT `avgprice` FROM `catalog_marketplace_data` WHERE `sprite` = @spriteId LIMIT 1",
            new { spriteId }) ?? 0;
        session.Send(new MarketplaceItemStatsComposer(itemId, spriteId, avgPrice, _marketplaceManager));
        return Task.CompletedTask;
    }
}