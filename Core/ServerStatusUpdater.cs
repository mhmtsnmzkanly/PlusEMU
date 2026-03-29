using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Core;

public class ServerStatusUpdater : IDisposable, IServerStatusUpdater
{
    private const int FlushInSeconds = 1;
    private const int ReconcileInSeconds = 30;
    private readonly ILogger<ServerStatusUpdater> _logger;
    private readonly IDatabase _database;
    private readonly IGameClientManager _gameClientManager;
    private readonly IRoomManager _roomManager;
    private readonly IServerStatusSignal _serverStatusSignal;
    private readonly IServerRuntimeState _serverRuntimeState;
    private int _lastPersistedUsers = -1;
    private int _lastPersistedRooms = -1;
    private DateTime _lastPersistedAt = DateTime.MinValue;

    public ServerStatusUpdater(ILogger<ServerStatusUpdater> logger, IDatabase database, IGameClientManager gameClientManager, IRoomManager roomManager, IServerStatusSignal serverStatusSignal, IServerRuntimeState serverRuntimeState)
    {
        _logger = logger;
        _database = database;
        _gameClientManager = gameClientManager;
        _roomManager = roomManager;
        _serverStatusSignal = serverStatusSignal;
        _serverRuntimeState = serverRuntimeState;
    }

    private Timer? _timer;

    public void Dispose()
    {
        using var db = _database.Connection();
        db.Execute("UPDATE `server_status` SET `users_online` = '0', `loaded_rooms` = '0'");
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Init()
    {
        _timer = new(OnTick, null, TimeSpan.FromSeconds(FlushInSeconds), TimeSpan.FromSeconds(FlushInSeconds));
        Console.Title = "PlusEMU - 0 users online - 0 rooms loaded - 0 day(s) 0 hour(s) uptime";
        _logger.LogInformation("Server Status Updater has been started.");
    }

    public void OnTick(object? obj)
    {
        try
        {
            UpdateServerStatus();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Server status update tick failed.");
        }
    }

    private void UpdateServerStatus()
    {
        var now = DateTime.Now;
        var uptime = now - _serverRuntimeState.StartedAt;
        var usersOnline = _gameClientManager.Count;
        var roomCount = _roomManager.Count;
        Console.Title = $"PlusEMU - {usersOnline} users online - {roomCount} rooms loaded - {uptime.Days} day(s) {uptime.Hours} hour(s) uptime";

        if (!ShouldPersist(usersOnline, roomCount, now))
            return;

        using var db = _database.Connection();
        db.Execute(
            "UPDATE `server_status` SET `users_online` = @users, `loaded_rooms` = @loadedRooms LIMIT 1",
            new { users = usersOnline, loadedRooms = roomCount });
        _lastPersistedUsers = usersOnline;
        _lastPersistedRooms = roomCount;
        _lastPersistedAt = now;
    }

    private bool ShouldPersist(int usersOnline, int roomCount, DateTime now)
    {
        if (_serverStatusSignal.ConsumeDirty())
            return true;

        if (usersOnline != _lastPersistedUsers || roomCount != _lastPersistedRooms)
            return true;

        return now - _lastPersistedAt >= TimeSpan.FromSeconds(ReconcileInSeconds);
    }
}
