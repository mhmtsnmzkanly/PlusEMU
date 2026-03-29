using System.Diagnostics;
using System.Globalization;
using System.Text;
using Dapper;
using Microsoft.Extensions.Options;
using NLog;
using Plus.Communication.Encryption;
using Plus.Communication.Flash;
using Plus.Communication.Nitro;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.RCON;
using Plus.Core;
using Plus.Core.FigureData;
using Plus.Core.Language;
using Plus.Core.Settings;
using Plus.Database;
using Plus.HabboHotel;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.UserData;

namespace Plus;

public class PlusEnvironment : IPlusEnvironment
{
    public const string PrettyVersion = "PlusEMU";
    public const string PrettyBuild = "3.4.3.0";
    private static readonly ILogger Log = LogManager.GetLogger("Plus.PlusEnvironment");

    private static IGame _game = null!;
    private static IGameClientManager _gameClientManager = null!;
    private static IRoomManager _roomManager = null!;
    private static ILanguageManager _languageManager = null!;
    private static IDatabase _database = null!;
    private static IFlashServer _flashServer = null!;
    private readonly ISettingsManager _settingsManager;
    private readonly IFigureDataManager _figureManager;
    private static IRconSocket _rcon = null!;
    private readonly IItemDataManager _itemDataManager;
    private static INitroServer _nitroServer = null!;
    private static IServerStatusUpdater _serverStatusUpdater = null!;

    public static DateTime ServerStarted;

    private static readonly List<char> Allowedchars = new(new[]
    {
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l',
        'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x',
        'y', 'z', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-', '.'
    });

    private readonly IEnumerable<IStartable> _startableTasks;
    private readonly RconConfiguration _rconConfiguration;

    public PlusEnvironment(IDatabase database,
        ILanguageManager languageManager,
        ISettingsManager settingsManager,
        IFigureDataManager figureDataManager,
        IGame game,
        IGameClientManager gameClientManager,
        IRoomManager roomManager,
        IEnumerable<IStartable> startableTasks,
        IRconSocket rconSocket,
        IOptions<RconConfiguration> rconConfiguration,
        IItemDataManager itemDataManager,
        IFlashServer flashServer,
        INitroServer nitroServer,
        IServerStatusUpdater serverStatusUpdater)
    {
        _database = database;
        _languageManager = languageManager;
        _settingsManager = settingsManager;
        _figureManager = figureDataManager;
        _game = game;
        _gameClientManager = gameClientManager;
        _roomManager = roomManager;
        _startableTasks = startableTasks;
        _rcon = rconSocket;
        _flashServer = flashServer;
        _nitroServer = nitroServer;
        _serverStatusUpdater = serverStatusUpdater;
        _rconConfiguration = rconConfiguration.Value;
        _itemDataManager = itemDataManager;
    }

    public async Task<bool> Start()
    {
        ServerStarted = DateTime.Now;
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine();
        Console.WriteLine("                     ____  __           ________  _____  __");
        Console.WriteLine(@"                    / __ \/ /_  _______/ ____/  |/  / / / /");
        Console.WriteLine("                   / /_/ / / / / / ___/ __/ / /|_/ / / / / ");
        Console.WriteLine("                  / ____/ / /_/ (__  ) /___/ /  / / /_/ /  ");
        Console.WriteLine(@"                 /_/   /_/\__,_/____/_____/_/  /_/\____/ ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"                                {PrettyVersion} <Build {PrettyBuild}>");
        Console.WriteLine("                                http://PlusIndustry.com");
        Console.WriteLine("");
        Console.Title = "Loading PlusEMU";
        Console.WriteLine("");
        Console.WriteLine("");
        try
        {
            if (!_database.IsConnected())
            {
                Log.Error("Failed to Connect to the specified MySQL server.");
                Console.ReadKey(true);
                return false;
            }
            Log.Info("Connected to Database!");

            //Reset our statistics first.
            await ResetStatistics();

            //Get the configuration & Game set.
            await _languageManager.Reload();
            await _settingsManager.Reload();
            _figureManager.Init();

            //Have our encryption ready.
            HabboEncryptionV2.Initialize(new());

            //Make sure Rcon is connected before we allow clients to Connect.
            _rcon.Init(_rconConfiguration.Hostname, _rconConfiguration.Port, _rconConfiguration.AllowedAddresses);

            //Accept connections.
            _flashServer.Start();
            _nitroServer.Start();

            _itemDataManager.Init();
            // Allow services to self initialize
            foreach (var task in _startableTasks)
                await task.Start();

            await _game.Init();
            _game.StartGameLoop();
            var timeUsed = DateTime.Now - ServerStarted;
            Console.WriteLine();
            Log.Info($"EMULATOR -> READY! ({timeUsed.Seconds} s, {timeUsed.Milliseconds} ms)");
        }
#pragma warning disable CS0168 // The variable 'e' is declared but never used
        catch (KeyNotFoundException e)
#pragma warning restore CS0168 // The variable 'e' is declared but never used
        {
            Log.Error("Please check your configuration file - some values appear to be missing.");
            Log.Error("Press any key to shut down ...");
            Console.ReadKey(true);
            return false;
        }
        catch (InvalidOperationException e)
        {
            Log.Error($"Failed to initialize PlusEmulator: {e.Message}");
            Log.Error("Press any key to shut down ...");
            Console.ReadKey(true);
            return false;
        }
        catch (Exception e)
        {
            Log.Error($"Fatal error during startup: {e}");
            Log.Error("Press a key to exit");
            Console.ReadKey();
            return false;
        }

        return true;
    }

    private async Task ResetStatistics()
    {
        using var connection = _database.Connection();
        await connection.ExecuteAsync("TRUNCATE `catalog_marketplace_data`");
        await connection.ExecuteAsync("UPDATE `rooms` SET `users_now` = '0' WHERE `users_now` > '0';");
        await connection.ExecuteAsync("UPDATE `users` SET `online` = false WHERE `online` = true");
        await connection.ExecuteAsync("UPDATE `server_status` SET `users_online` = '0', `loaded_rooms` = '0'");
    }

    public static string FilterFigure(string figure)
    {
        foreach (var character in figure)
        {
            if (!IsValid(character))
                return "sh-3338-93.ea-1406-62.hr-831-49.ha-3331-92.hd-180-7.ch-3334-93-1408.lg-3337-92.ca-1813-62";
        }
        return figure;
    }

    private static bool IsValid(char character) => Allowedchars.Contains(character);

    public static void BroadcastAlert(string message)
    {
        _gameClientManager.SendPacket(new BroadcastMessageAlertComposer($"{_languageManager.TryGetValue("server.console.alert")}\n\n{message}"));
    }


    public static void PerformShutDown(string? reason = null)
    {
        Log.Info("Server shutting down... Reason: {reason}", string.IsNullOrWhiteSpace(reason) ? "Unspecified" : reason);
        Console.Title = "PLUSEMU: SHUTTING DOWN!";
        _gameClientManager.SendPacket(new BroadcastMessageAlertComposer(_languageManager.TryGetValue("server.shutdown.message")));
        _game.StopGameLoop();
        Thread.Sleep(2500);
        _flashServer.Stop();
        _nitroServer.Stop();
        _rcon.Stop();
        _serverStatusUpdater.Dispose();
        _gameClientManager.CloseAll(); //Close all connections
        _roomManager.Dispose(); //Stop the game loop.
        if (!Debugger.IsAttached)
        {
            using var connection = _database.Connection();
            connection.Execute("TRUNCATE `catalog_marketplace_data`");
            connection.Execute("UPDATE `users` SET `online` = false, `auth_ticket` = NULL");
            connection.Execute("UPDATE `rooms` SET `users_now` = '0' WHERE `users_now` > '0'");
            connection.Execute("UPDATE `server_status` SET `users_online` = '0', `loaded_rooms` = '0'");
        }
        Log.Info("PlusEMU has successfully shutdown.");
        Thread.Sleep(1000);
        Environment.Exit(0);
    }

}
