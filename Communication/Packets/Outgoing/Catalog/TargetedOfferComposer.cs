using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Outgoing.Catalog;

public sealed class TargetedOfferComposer : IServerPacket
{
    private readonly TargetedOffer _offer;
    private readonly TargetedOfferPurchaseState _purchaseState;

    public TargetedOfferComposer(TargetedOffer offer, TargetedOfferPurchaseState purchaseState)
    {
        _offer = offer;
        _purchaseState = purchaseState;
    }

    public uint MessageId => ServerPacketHeader.TargetedOfferComposer;

    public void Compose(IOutgoingPacket packet)
    {
        packet.WriteInteger(_purchaseState.State);
        packet.WriteInteger(_offer.Id);
        packet.WriteString(_offer.Identifier);
        packet.WriteString(_offer.Identifier);
        packet.WriteInteger(_offer.PriceInCredits);
        packet.WriteInteger(_offer.PriceInActivityPoints);
        packet.WriteInteger(_offer.ActivityPointsType);
        packet.WriteInteger(Math.Max(_offer.PurchaseLimit - _purchaseState.Amount, 0));
        packet.WriteInteger(Math.Max(_offer.EndTimestamp - (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 0));
        packet.WriteString(_offer.Title);
        packet.WriteString(_offer.Description);
        packet.WriteString(_offer.ImageUrl);
        packet.WriteString(_offer.Icon);
        packet.WriteInteger(0);
        packet.WriteInteger(_offer.Variables.Length);
        foreach (var variable in _offer.Variables)
            packet.WriteString(variable);
    }
}
