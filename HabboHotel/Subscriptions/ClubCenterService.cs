using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Subscriptions;

internal sealed class ClubCenterService : IClubCenterService
{
    private readonly ISubscriptionManager _subscriptionManager;
    private readonly ICatalogManager _catalogManager;

    public ClubCenterService(ISubscriptionManager subscriptionManager, ICatalogManager catalogManager)
    {
        _subscriptionManager = subscriptionManager;
        _catalogManager = catalogManager;
    }

    public Task SendClubCenterData(GameClient session, int windowId)
    {
        if (session.GetHabbo() is not { } habbo)
            return Task.CompletedTask;

        _subscriptionManager.TryGetSubscriptionData(habbo.VipRank, out var subscriptionData);

        var firstSubscriptionDate = DateTimeOffset.FromUnixTimeSeconds((long)habbo.AccountCreated).UtcDateTime.ToString("dd-MM-yyyy");
        var currentHcStreak = habbo.Vip ? Math.Max(1, (int)Math.Floor((DateTimeOffset.UtcNow.ToUnixTimeSeconds() - habbo.AccountCreated) / 86400d)) : 0;
        var minutesUntilPayday = GetMinutesUntilPayday();
        var kickbackPercentage = habbo.Vip ? 0.10 : 0.0;
        var streakReward = subscriptionData?.Credits ?? 0;

        session.Send(new ClubCenterDataComposer(
            currentHcStreak,
            firstSubscriptionDate,
            kickbackPercentage,
            0,
            0,
            0,
            streakReward,
            0,
            minutesUntilPayday,
            windowId));

        return Task.CompletedTask;
    }

    public Task SendClubGifts(GameClient session)
    {
        if (session.GetHabbo() is not { } habbo)
            return Task.CompletedTask;

        var daysAsHc = habbo.Vip ? Math.Max(1, (int)Math.Floor((DateTimeOffset.UtcNow.ToUnixTimeSeconds() - habbo.AccountCreated) / 86400d)) : 0;
        var availableGifts = habbo.Vip ? 1 : 0;
        var daysTillNextGift = habbo.Vip ? 0 : 30;
        var giftItems = _catalogManager.Pages
            .Where(page => page.Layout.Equals("club_gifts", StringComparison.OrdinalIgnoreCase) || page.Layout.Equals("clubgift", StringComparison.OrdinalIgnoreCase))
            .SelectMany(page => page.Items.Values)
            .OrderBy(item => item.Id)
            .ToList();

        session.Send(new ClubGiftsComposer(daysTillNextGift, availableGifts, daysAsHc, giftItems));
        return Task.CompletedTask;
    }

    private static int GetMinutesUntilPayday()
    {
        var now = DateTime.UtcNow;
        var nextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
        return Math.Max(0, (int)Math.Ceiling((nextMonth - now).TotalMinutes));
    }
}
