using Dapper;
using Plus.Communication.Packets.Outgoing.Marketplace;
using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Marketplace;

internal class GetMarketplaceItemStatsEvent : IPacketEvent
{
    private readonly IDatabase _database;

    public GetMarketplaceItemStatsEvent(IDatabase database)
    {
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var itemId = packet.ReadInt();
        var spriteId = packet.ReadUInt();
        using var db = _database.Connection();
        var avgPrice = db.QueryFirstOrDefault<int?>(
            "SELECT `avgprice` FROM `catalog_marketplace_data` WHERE `sprite` = @spriteId LIMIT 1",
            new { spriteId }) ?? 0;
        session.Send(new MarketplaceItemStatsComposer(itemId, spriteId, avgPrice));
        return Task.CompletedTask;
    }
}