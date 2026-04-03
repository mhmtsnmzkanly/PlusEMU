using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Communication.Flash;
using Plus.Communication.Nitro;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.RCON;
using Plus.Core.Language;
using Plus.Database;
using Plus.HabboHotel;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Core;

internal sealed class RuntimeControlService : IRuntimeControlService
{
    private readonly ILogger<RuntimeControlService> _logger;
    private readonly IGame _game;
    private readonly IGameClientManager _gameClientManager;
    private readonly IRoomManager _roomManager;
    private readonly ILanguageManager _languageManager;
    private readonly IDatabase _database;
    private readonly IFlashServer _flashServer;
    private readonly INitroServer _nitroServer;
    private readonly IRconSocket _rcon;
    private readonly IServerStatusUpdater _serverStatusUpdater;

    public RuntimeControlService(
        ILogger<RuntimeControlService> logger,
        IGame game,
        IGameClientManager gameClientManager,
        IRoomManager roomManager,
        ILanguageManager languageManager,
        IDatabase database,
        IFlashServer flashServer,
        INitroServer nitroServer,
        IRconSocket rcon,
        IServerStatusUpdater serverStatusUpdater)
    {
        _logger = logger;
        _game = game;
        _gameClientManager = gameClientManager;
        _roomManager = roomManager;
        _languageManager = languageManager;
        _database = database;
        _flashServer = flashServer;
        _nitroServer = nitroServer;
        _rcon = rcon;
        _serverStatusUpdater = serverStatusUpdater;
    }

    public void BroadcastAlert(string message)
    {
        _gameClientManager.SendPacket(new BroadcastMessageAlertComposer($"{_languageManager.Require("server.console.alert")}\n\n{message}"));
    }

    public void PerformShutdown(string? reason = null)
    {
        _logger.LogInformation("Server shutting down... Reason: {reason}", string.IsNullOrWhiteSpace(reason) ? "Unspecified" : reason);
        Console.Title = "PLUSEMU: SHUTTING DOWN!";
        _gameClientManager.SendPacket(new BroadcastMessageAlertComposer(_languageManager.Require("server.shutdown.message")));
        _game.StopGameLoop();
        Thread.Sleep(2500);
        _flashServer.Stop();
        _nitroServer.Stop();
        _rcon.Stop();
        _serverStatusUpdater.Dispose();
        _gameClientManager.CloseAll();
        _roomManager.Dispose();
        if (!Debugger.IsAttached)
        {
            using var connection = _database.Connection();
            connection.Execute("TRUNCATE `catalog_marketplace_data`");
            connection.Execute("UPDATE `users` SET `online` = false, `auth_ticket` = ''");
            connection.Execute("UPDATE `rooms` SET `users_now` = '0' WHERE `users_now` > '0'");
            connection.Execute("UPDATE `server_status` SET `users_online` = '0', `loaded_rooms` = '0'");
        }
        _logger.LogInformation("PlusEMU has successfully shutdown.");
        Thread.Sleep(1000);
        Environment.Exit(0);
    }
}
