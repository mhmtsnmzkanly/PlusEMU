using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.Catalog.Utilities;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.Communication.Packets.Outgoing.Catalog;

public class CatalogPageComposer : IServerPacket
{
    private readonly CatalogPage _page;
    private readonly string _mode;
    private readonly ICatalogManager _catalogManager;
    public uint MessageId => ServerPacketHeader.CatalogPageComposer;

    public CatalogPageComposer(CatalogPage page, string mode, ICatalogManager catalogManager)
    {
        _page = page;
        _mode = mode;
        _catalogManager = catalogManager;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_page.Id);
        packet.WriteString(_mode);
        packet.WriteString(_page.Layout ?? string.Empty);
        packet.WriteInteger(_page.PageStringsList1.Count);
        foreach (var s in _page.PageStringsList1) packet.WriteString(s ?? string.Empty);
        packet.WriteInteger(_page.PageStringsList2.Count);
        foreach (var s in _page.PageStringsList2) packet.WriteString(s ?? string.Empty);
        if (!string.Equals(_page.Layout, "frontpage", StringComparison.Ordinal) && !string.Equals(_page.Layout, "club_buy", StringComparison.Ordinal))
        {
            packet.WriteInteger(_page.Items.Count);
            foreach (var item in _page.Items.Values)
            {
                packet.WriteInteger(item.Id);
                packet.WriteString(item.CatalogName ?? string.Empty);
                packet.WriteBoolean(false); //IsRentable
                packet.WriteInteger(item.CostCredits);
                if (item.CostDiamonds > 0)
                {
                    packet.WriteInteger(item.CostDiamonds);
                    packet.WriteInteger(5); // Diamonds
                }
                else
                {
                    packet.WriteInteger(item.CostPixels);
                    packet.WriteInteger(0); // Type of PixelCost
                }
                packet.WriteBoolean(ItemUtility.CanGiftItem(item));
                if (item.Definition.InteractionType == InteractionType.Deal || item.Definition.InteractionType == InteractionType.Roomdeal)
                {
                    CatalogDeal? deal = null;
                    if (!_catalogManager.TryGetDeal(item.Definition.BehaviourData, out deal))
                        packet.WriteInteger(0); //Count
                    else
                    {
                        packet.WriteInteger(deal.ItemDataList.Count);
                        foreach (var dealItem in deal.ItemDataList.ToList())
                        {
                            packet.WriteString(dealItem.Definition.Type.ToString());
                            packet.WriteInteger(dealItem.Definition.SpriteId);
                            packet.WriteString("");
                            packet.WriteInteger(dealItem.Amount);
                            packet.WriteBoolean(false);
                        }
                    }
                }
                else
                {
                    packet.WriteInteger(string.IsNullOrEmpty(item.Badge) ? 1 : 2); //Count 1 item if there is no badge, otherwise count as 2.
                    if (!string.IsNullOrEmpty(item.Badge))
                    {
                        packet.WriteString("b");
                        packet.WriteString(item.Badge);
                    }
                    packet.WriteString(item.Definition.Type.ToCharCode().ToLower());
                    if (item.Definition.Type.ToString().ToLower() == "b")
                    {
                        //This is just a badge, append the name.
                        packet.WriteString(item.Definition.ItemName ?? string.Empty);
                    }
                    else
                    {
                        packet.WriteInteger(item.Definition.SpriteId);
                        if (item.Definition.InteractionType == InteractionType.Wallpaper || item.Definition.InteractionType == InteractionType.Floor || item.Definition.InteractionType == InteractionType.Landscape)
                            packet.WriteString((item.CatalogName ?? string.Empty).Split('_').ElementAtOrDefault(2) ?? string.Empty);
                        else if (item.Definition.InteractionType == InteractionType.Bot) //Bots
                        {
                            CatalogBot? catalogBot = null;
                            if (!_catalogManager.TryGetBot(item.ItemId, out catalogBot))
                                packet.WriteString("hd-180-7.ea-1406-62.ch-210-1321.hr-831-49.ca-1813-62.sh-295-1321.lg-285-92");
                            else
                                packet.WriteString(catalogBot.Figure ?? string.Empty);
                        }
                        else if (item.ExtraData != null) packet.WriteString(item.ExtraData);
                        else packet.WriteString(string.Empty);
                        packet.WriteInteger(item.Amount);
                        packet.WriteBoolean(item.IsLimited); // IsLimited
                        if (item.IsLimited)
                        {
                            packet.WriteUInteger(item.LimitedEditionStack);
                            packet.WriteUInteger(item.LimitedEditionStack - item.LimitedEditionSells);
                        }
                    }
                }
                packet.WriteInteger(0); //club_level
                packet.WriteBoolean(ItemUtility.CanSelectAmount(item));
                packet.WriteBoolean(false); // TODO: Figure out
                packet.WriteString(""); //previewImage -> e.g; catalogue/pet_lion.png
            }
        }
        else
            packet.WriteInteger(0);
        packet.WriteInteger(-1);
        packet.WriteBoolean(false);
        packet.WriteInteger(_catalogManager.Promotions.ToList().Count); //Count
        foreach (var promotion in _catalogManager.Promotions.ToList())
        {
            packet.WriteInteger(promotion.Id);
            packet.WriteString(promotion.Title ?? string.Empty);
            packet.WriteString(promotion.Image ?? string.Empty);
            packet.WriteInteger(promotion.Unknown);
            packet.WriteString(promotion.PageLink ?? string.Empty);
            packet.WriteInteger(promotion.ParentId);
        }
    }
}
