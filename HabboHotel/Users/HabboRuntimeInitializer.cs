using Plus.Core.Settings;
using Plus.Database;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Subscriptions;
using Plus.HabboHotel.Users.Clothing;
using Plus.HabboHotel.Users.Effects;
using Plus.HabboHotel.Users.Process;

namespace Plus.HabboHotel.Users;

internal class HabboRuntimeInitializer : IHabboRuntimeInitializer
{
    private readonly IDatabase _database;
    private readonly ISettingsManager _settingsManager;
    private readonly ISubscriptionManager _subscriptionManager;
    private readonly IAchievementService _achievementService;

    public HabboRuntimeInitializer(
        IDatabase database,
        ISettingsManager settingsManager,
        ISubscriptionManager subscriptionManager,
        IAchievementService achievementService)
    {
        _database = database;
        _settingsManager = settingsManager;
        _subscriptionManager = subscriptionManager;
        _achievementService = achievementService;
    }

    public void EnsureVisualComponents(Habbo habbo)
    {
        if (habbo.Effects == null)
        {
            habbo.Effects = new EffectsComponent();
            habbo.Effects.Init(habbo, _database);
        }

        if (habbo.Clothing == null)
        {
            habbo.Clothing = new ClothingComponent();
            habbo.Clothing.Init(habbo, _database);
        }
    }

    public void EnsureProcessComponent(Habbo habbo)
    {
        if (habbo.HasProcessComponent)
            return;

        var process = new ProcessComponent();
        if (!process.Init(habbo, _database, _settingsManager, _subscriptionManager, _achievementService))
            return;

        habbo.AttachProcess(process, _database);
    }
}
