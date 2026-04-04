using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.Communication.Packets.Outgoing.Catalog;

public class ClubGiftsComposer : IServerPacket
{
    private readonly int _daysTillNextGift;
    private readonly int _availableGifts;
    private readonly int _daysAsHc;
    private readonly IReadOnlyCollection<CatalogItem> _items;

    public uint MessageId => ServerPacketHeader.ClubGiftsComposer;

    public ClubGiftsComposer(int daysTillNextGift, int availableGifts, int daysAsHc, IReadOnlyCollection<CatalogItem> items)
    {
        _daysTillNextGift = daysTillNextGift;
        _availableGifts = availableGifts;
        _daysAsHc = daysAsHc;
        _items = items;
    }

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_daysTillNextGift);
        packet.WriteInteger(_availableGifts);
        packet.WriteInteger(_items.Count);
        foreach (var item in _items)
        {
            packet.WriteInteger(item.Id);
            packet.WriteString(item.CatalogName ?? string.Empty);
            packet.WriteBoolean(false);
            packet.WriteInteger(item.CostCredits);
            packet.WriteInteger(item.CostDiamonds > 0 ? item.CostDiamonds : item.CostPixels);
            packet.WriteInteger(item.CostDiamonds > 0 ? 5 : 0);
            packet.WriteBoolean(true);
            packet.WriteInteger(1);
            packet.WriteString(item.Definition.Type.ToCharCode().ToLower());
            packet.WriteInteger(item.Definition.SpriteId);
            packet.WriteString(item.ExtraData ?? string.Empty);
            packet.WriteInteger(item.Amount);
            packet.WriteBoolean(item.IsLimited);
            packet.WriteInteger(0);
            packet.WriteBoolean(false);
            packet.WriteBoolean(false);
            packet.WriteString(string.Empty);
        }
        packet.WriteInteger(_items.Count);
        foreach (var item in _items)
        {
            var daysRequired = 0;
            int.TryParse(item.ExtraData, out daysRequired);
            packet.WriteInteger(item.Id);
            packet.WriteBoolean(false);
            packet.WriteInteger(daysRequired);
            packet.WriteBoolean(daysRequired <= _daysAsHc);
        }
    }
}
