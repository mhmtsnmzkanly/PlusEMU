using System.Diagnostics;
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
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;

namespace Plus;

public class PlusEnvironment : IPlusEnvironment
{
    public const string PrettyVersion = "PlusEMU";
    public const string PrettyBuild = "3.4.3.0";
    private static readonly ILogger Log = LogManager.GetLogger("Plus.PlusEnvironment");

    private readonly IDatabase _database;
    private readonly ILanguageManager _languageManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IFigureDataManager _figureManager;
    private readonly IGame _game;
    private readonly IRconSocket _rcon;
    private readonly IItemDataManager _itemDataManager;
    private readonly IFlashServer _flashServer;
    private readonly INitroServer _nitroServer;
    private readonly IServerRuntimeState _serverRuntimeState;
    private readonly IEnumerable<IStartable> _startableTasks;
    private readonly RconConfiguration _rconConfiguration;
    private readonly FlashServerConfiguration _flashConfiguration;
    private readonly NitroServerConfiguration _nitroConfiguration;

    public PlusEnvironment(IDatabase database,
        ILanguageManager languageManager,
        ISettingsManager settingsManager,
        IFigureDataManager figureDataManager,
        IGame game,
        IServerRuntimeState serverRuntimeState,
        IEnumerable<IStartable> startableTasks,
        IRconSocket rconSocket,
        IOptions<RconConfiguration> rconConfiguration,
        IOptions<FlashServerConfiguration> flashConfiguration,
        IOptions<NitroServerConfiguration> nitroConfiguration,
        IItemDataManager itemDataManager,
        IFlashServer flashServer,
        INitroServer nitroServer)
    {
        _database = database;
        _languageManager = languageManager;
        _settingsManager = settingsManager;
        _figureManager = figureDataManager;
        _game = game;
        _serverRuntimeState = serverRuntimeState;
        _startableTasks = startableTasks;
        _rcon = rconSocket;
        _flashServer = flashServer;
        _nitroServer = nitroServer;
        _rconConfiguration = rconConfiguration.Value;
        _flashConfiguration = flashConfiguration.Value;
        _nitroConfiguration = nitroConfiguration.Value;
        _itemDataManager = itemDataManager;
    }

    public async Task<bool> Start()
    {
        _serverRuntimeState.MarkStarted(DateTime.Now);
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
                WaitForExitKeyIfInteractive();
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
            EnsureServerStarted(_flashServer.Start(), "Flash", _flashConfiguration.Hostname, _flashConfiguration.Port);
            EnsureServerStarted(_nitroServer.Start(), "Nitro", _nitroConfiguration.Hostname, _nitroConfiguration.Port);

            _itemDataManager.Init();
            // Allow services to self initialize
            foreach (var task in _startableTasks)
                await task.Start();

            await _game.Init();
            _game.StartGameLoop();
            var timeUsed = DateTime.Now - _serverRuntimeState.StartedAt;
            Console.WriteLine();
            Log.Info($"EMULATOR -> READY! ({timeUsed.Seconds} s, {timeUsed.Milliseconds} ms)");
        }
#pragma warning disable CS0168 // The variable 'e' is declared but never used
        catch (KeyNotFoundException e)
#pragma warning restore CS0168 // The variable 'e' is declared but never used
        {
            Log.Error("Please check your configuration file - some values appear to be missing.");
            Log.Error("Press any key to shut down ...");
            WaitForExitKeyIfInteractive();
            return false;
        }
        catch (InvalidOperationException e)
        {
            Log.Error($"Failed to initialize PlusEmulator: {e.Message}");
            Log.Error("Press any key to shut down ...");
            WaitForExitKeyIfInteractive();
            return false;
        }
        catch (Exception e)
        {
            Log.Error($"Fatal error during startup: {e}");
            Log.Error("Press a key to exit");
            WaitForExitKeyIfInteractive();
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

    private static void WaitForExitKeyIfInteractive()
    {
        try
        {
            if (!Console.IsInputRedirected)
                Console.ReadKey(true);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static void EnsureServerStarted(bool started, string serverName, string host, int port)
    {
        if (!started)
            throw new InvalidOperationException($"{serverName} server failed to bind on {host}:{port}.");
    }
}
