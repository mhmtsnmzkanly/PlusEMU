using Plus.HabboHotel.Catalog.Marketplace;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Marketplace;

public class MarketPlaceOffersComposer : IServerPacket
{
    private readonly Dictionary<uint, MarketOffer> _dictionary;
    private readonly Dictionary<uint, int> _dictionary2;
    private readonly IMarketplaceManager _marketplaceManager;
    public uint MessageId => ServerPacketHeader.MarketPlaceOffersComposer;

    public MarketPlaceOffersComposer(Dictionary<uint, MarketOffer> dictionary, Dictionary<uint, int> dictionary2, IMarketplaceManager marketplaceManager)
    {
        _dictionary = dictionary;
        _dictionary2 = dictionary2;
        _marketplaceManager = marketplaceManager;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_dictionary.Count);
        foreach (var (_, value) in _dictionary)
        {
            packet.WriteUInteger(value.OfferId);
            packet.WriteInteger(1); //State
            packet.WriteInteger(1);
            packet.WriteUInteger(value.SpriteId);
            packet.WriteInteger(256);
            packet.WriteString("");
            packet.WriteUInteger(value.LimitedNumber);
            packet.WriteUInteger(value.LimitedStack);
            packet.WriteInteger(value.TotalPrice);
            packet.WriteInteger(0);
            packet.WriteInteger(_marketplaceManager.AvgPriceForSprite((int)value.SpriteId));
            packet.WriteInteger(_dictionary2[value.SpriteId]);
        }
        packet.WriteInteger(_dictionary.Count); //Item count to show how many were found.
    }
}