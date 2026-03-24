using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Catalog;

public class GetCatalogPageEvent : IPacketEvent
{
    private readonly ICatalogManager _catalogManager;

    public GetCatalogPageEvent(ICatalogManager catalogManager)
    {
        _catalogManager = catalogManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var pageId = packet.ReadInt();
        packet.ReadInt();
        var cataMode = packet.ReadString();
        if (!_catalogManager.TryGetPage(pageId, out var page))
            return Task.CompletedTask;
        if (!page.Enabled || !page.Visible || page.MinimumRank > habbo.Rank || page.MinimumVip > habbo.VipRank && habbo.Rank == 1)
            return Task.CompletedTask;
        session.Send(new CatalogPageComposer(page, cataMode));
        return Task.CompletedTask;
    }
}
