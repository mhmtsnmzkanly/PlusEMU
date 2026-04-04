using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Catalog;

[Singleton]
public interface ITargetedOfferManager
{
    bool TryGetActiveOffer(out TargetedOffer? offer);
    bool TryGetOffer(int offerId, out TargetedOffer? offer);
}
