using Dapper;
using NLog;
using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.HabboHotel.Achievements;
using Plus.Core.Settings;
using Plus.HabboHotel.Subscriptions;
using Plus.Database;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;

namespace Plus.HabboHotel.Users.Process;

internal sealed class ProcessComponent
{
    private static readonly ILogger Log = LogManager.GetLogger("Plus.HabboHotel.Users.Process.ProcessComponent");

    /// <summary>
    /// How often the timer should execute.
    /// </summary>
    private static readonly int _runtimeInSec = 60;

    /// <summary>
    /// Used for disposing the ProcessComponent safely.
    /// </summary>
    private readonly AutoResetEvent _resetEvent = new(true);

    /// <summary>
    /// Enable/Disable the timer WITHOUT disabling the timer itself.
    /// </summary>
    private bool _disabled;

    /// <summary>
    /// Player to update, handle, change etc.
    /// </summary>
    private Habbo? _player;
    private IDatabase? _database;
    private ISettingsManager? _settingsManager;
    private ISubscriptionManager? _subscriptionManager;
    private IAchievementService? _achievementService;

    /// <summary>
    /// ThreadPooled Timer.
    /// </summary>
    private Timer? _timer;

#pragma warning disable CS0414 // The field 'ProcessComponent._timerLagging' is assigned but its value is never used
    /// <summary>
    /// Checks if the timer is lagging behind (server can't keep up).
    /// </summary>
    private bool _timerLagging;
#pragma warning restore CS0414 // The field 'ProcessComponent._timerLagging' is assigned but its value is never used

    /// <summary>
    /// Prevents the timer from overlapping itself.
    /// </summary>
    private bool _timerRunning;

    /// <summary>
    /// Initializes the ProcessComponent.
    /// </summary>
    /// <param name="player">Player.</param>
    public bool Init(Habbo player, IDatabase database, ISettingsManager settingsManager, ISubscriptionManager subscriptionManager, IAchievementService achievementService)
    {
        if (player == null)
            return false;
        if (_player != null)
            return false;
        _player = player;
        _database = database;
        _settingsManager = settingsManager;
        _subscriptionManager = subscriptionManager;
        _achievementService = achievementService;
        _timer = new(Run, null, _runtimeInSec * 1000, _runtimeInSec * 1000);
        return true;
    }

    /// <summary>
    /// Called for each time the timer ticks.
    /// </summary>
    /// <param name="state"></param>
    public void Run(object? state)
    {
        try
        {
            if (_disabled)
                return;
            var player = _player;
            var database = _database;
            var settingsManager = _settingsManager;
            var subscriptionManager = _subscriptionManager;
            var achievementService = _achievementService;
            if (player == null || database == null || settingsManager == null || subscriptionManager == null || achievementService == null)
                return;
            if (_timerRunning)
            {
                _timerLagging = true;
                Log.Warn($"<Player {player.Id}> Server can't keep up, Player timer is lagging behind.");
                return;
            }

            _timerRunning = true;
            _resetEvent.Reset();

            // BEGIN CODE
            if (player.TimeMuted > 0)
                player.TimeMuted -= 60;
            if (player.MessengerSpamTime > 0)
                player.MessengerSpamTime -= 60;
            if (player.MessengerSpamTime <= 0)
                player.MessengerSpamCount = 0;
            player.TimeAfk += 1;
            if (player.HabboStats.RespectsTimestamp != DateTime.Today.ToString("MM/dd"))
            {
                player.HabboStats.RespectsTimestamp = DateTime.Today.ToString("MM/dd");
                var respectPoints = player.Rank == 1 && player.VipRank == 0 ? 10 : player.VipRank == 1 ? 15 : 20;
                using var db = database.Connection();
                db.Execute(
                    "UPDATE `user_statistics` SET `dailyRespectPoints` = @points, `dailyPetRespectPoints` = @points, `respectsTimestamp` = @ts WHERE `id` = @id LIMIT 1",
                    new { points = respectPoints, ts = DateTime.Today.ToString("MM/dd"), id = player.Id });
                player.HabboStats.DailyRespectPoints = respectPoints;
                player.HabboStats.DailyPetRespectPoints = respectPoints;
                if (player.TryGetClient(out var playerClient)) playerClient.Send(new UserObjectComposer(player));
            }
            if (player.GiftPurchasingWarnings < 15)
                player.GiftPurchasingWarnings = 0;
            if (player.MottoUpdateWarnings < 15)
                player.MottoUpdateWarnings = 0;
            if (player.ClothingUpdateWarnings < 15)
                player.ClothingUpdateWarnings = 0;
            if (player.TryGetClient(out var achievementClient))
                _ = achievementService.ProgressAchievement(achievementClient, "ACH_AllTimeHotelPresence", 1);
            player.CheckCreditsTimer(settingsManager, subscriptionManager);
            player.Effects?.CheckEffectExpiry(player, database);

            // END CODE
        }
        catch (Exception ex)
        {
            Log.Error(ex, "User process timer failed. UserId={userId}", _player?.Id ?? 0);
        }
        finally
        {
            _timerRunning = false;
            _timerLagging = false;
            _resetEvent.Set();
        }
    }

    /// <summary>
    /// Stops the timer and disposes everything.
    /// </summary>
    public void Dispose()
    {
        // Wait until any processing is complete first.
        try
        {
            _resetEvent.WaitOne(TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Timed wait for user process disposal failed.");
        }

        // Set the timer to disabled
        _disabled = true;

        // Dispose the timer to disable it.
        try
        {
            if (_timer != null)
                _timer.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Timer disposal failed for user process component.");
        }

        // Remove reference to the timer.
        _timer = null;

        // Null the player so we don't reference it here anymore
        _player = null;
    }
}
