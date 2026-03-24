using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Catalog;

public class CatalogIndexComposer : IServerPacket
{
    private readonly GameClient _session;
    private readonly ICollection<CatalogPage> _pages;

    public uint MessageId => ServerPacketHeader.CatalogIndexComposer;

    public CatalogIndexComposer(GameClient session, ICollection<CatalogPage> pages)
    {
        _session = session;
        _pages = pages;
    }

    public void Compose(IOutgoingPacket packet)
    {
        var habbo = _session.GetHabbo();
        WriteRootIndex(packet);
        foreach (var parent in _pages)
        {
            if (parent.ParentId != -1 || parent.MinimumRank > (habbo?.Rank ?? 0) || parent.MinimumVip > (habbo?.VipRank ?? 0) && (habbo?.Rank ?? 0) == 1)
                continue;
            WritePage(packet, parent, CalcTreeSize(_pages, parent.Id));
            foreach (var child in _pages)
            {
                if (child.ParentId != parent.Id || child.MinimumRank > (habbo?.Rank ?? 0) || child.MinimumVip > (habbo?.VipRank ?? 0) && (habbo?.Rank ?? 0) == 1)
                    continue;
                if (child.Enabled)
                    WritePage(packet, child, CalcTreeSize(_pages, child.Id));
                else
                    WriteNodeIndex(packet, child, CalcTreeSize(_pages, child.Id));
                foreach (var subChild in _pages)
                {
                    if (subChild.ParentId != child.Id || subChild.MinimumRank > (habbo?.Rank ?? 0))
                        continue;
                    WritePage(packet, subChild, 0);
                }
            }
        }
        packet.WriteBoolean(false);
        packet.WriteString("NORMAL");
    }

    public void WriteRootIndex(IOutgoingPacket packet)
    {
        packet.WriteBoolean(true);
        packet.WriteInteger(0);
        packet.WriteInteger(-1);
        packet.WriteString("root");
        packet.WriteString(string.Empty);
        packet.WriteInteger(0);
        packet.WriteInteger(CalcTreeSize(_pages, -1));
    }

    public void WriteNodeIndex(IOutgoingPacket packet, CatalogPage page, int treeSize)
    {
        packet.WriteBoolean(page.Visible);
        packet.WriteInteger(page.Icon);
        packet.WriteInteger(-1);
        packet.WriteString(page.Link ?? string.Empty);
        packet.WriteString(page.Caption ?? string.Empty);
        packet.WriteInteger(0);
        packet.WriteInteger(treeSize);
    }

    public void WritePage(IOutgoingPacket packet, CatalogPage page, int treeSize)
    {
        packet.WriteBoolean(page.Visible);
        packet.WriteInteger(page.Icon);
        packet.WriteInteger(page.Id);
        packet.WriteString(page.Link ?? string.Empty);
        packet.WriteString(page.Caption ?? string.Empty);
        packet.WriteInteger(page.ItemOffers.Count);
        foreach (var i in page.ItemOffers.Keys) packet.WriteInteger(i);
        packet.WriteInteger(treeSize);
    }

    public int CalcTreeSize(ICollection<CatalogPage> pages, int parentId)
    {
        var habbo = _session.GetHabbo();
        var i = 0;
        foreach (var page in pages)
        {
            if (page.MinimumRank > (habbo?.Rank ?? 0) || page.MinimumVip > (habbo?.VipRank ?? 0) && (habbo?.Rank ?? 0) == 1 || page.ParentId != parentId)
                continue;
            if (page.ParentId == parentId)
                i++;
        }
        return i;
    }
}
