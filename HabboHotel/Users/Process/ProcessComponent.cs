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
            if (_player == null)
                return;
            if (_timerRunning)
            {
                _timerLagging = true;
                Log.Warn($"<Player {_player.Id}> Server can't keep up, Player timer is lagging behind.");
                return;
            }
            _resetEvent.Reset();

            // BEGIN CODE
            if (_player.TimeMuted > 0)
                _player.TimeMuted -= 60;
            if (_player.MessengerSpamTime > 0)
                _player.MessengerSpamTime -= 60;
            if (_player.MessengerSpamTime <= 0)
                _player.MessengerSpamCount = 0;
            _player.TimeAfk += 1;
            if (_player.HabboStats.RespectsTimestamp != DateTime.Today.ToString("MM/dd"))
            {
                _player.HabboStats.RespectsTimestamp = DateTime.Today.ToString("MM/dd");
                var respectPoints = _player.Rank == 1 && _player.VipRank == 0 ? 10 : _player.VipRank == 1 ? 15 : 20;
                using var db = _database!.Connection();
                db.Execute(
                    "UPDATE `user_statistics` SET `dailyRespectPoints` = @points, `dailyPetRespectPoints` = @points, `respectsTimestamp` = @ts WHERE `id` = @id LIMIT 1",
                    new { points = respectPoints, ts = DateTime.Today.ToString("MM/dd"), id = _player.Id });
                _player.HabboStats.DailyRespectPoints = respectPoints;
                _player.HabboStats.DailyPetRespectPoints = respectPoints;
                if (_player.TryGetClient(out var playerClient)) playerClient.Send(new UserObjectComposer(_player));
            }
            if (_player.GiftPurchasingWarnings < 15)
                _player.GiftPurchasingWarnings = 0;
            if (_player.MottoUpdateWarnings < 15)
                _player.MottoUpdateWarnings = 0;
            if (_player.ClothingUpdateWarnings < 15)
                _player.ClothingUpdateWarnings = 0;
            if (_player.TryGetClient(out var achievementClient))
                _ = _achievementService!.ProgressAchievement(achievementClient, "ACH_AllTimeHotelPresence", 1);
            _player.CheckCreditsTimer(_settingsManager!, _subscriptionManager!);
            _player.Effects?.CheckEffectExpiry(_player, _database!);

            // END CODE

            // Reset the values
            _timerRunning = false;
            _timerLagging = false;
            _resetEvent.Set();
        }
        catch { }
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
        catch { } // give up

        // Set the timer to disabled
        _disabled = true;

        // Dispose the timer to disable it.
        try
        {
            if (_timer != null)
                _timer.Dispose();
        }
        catch { }

        // Remove reference to the timer.
        _timer = null;

        // Null the player so we don't reference it here anymore
        _player = null;
    }
}
