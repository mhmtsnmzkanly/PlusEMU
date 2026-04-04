using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Extensions.Logging;
using Plus.Core;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Plus.Communication.Flash;
using Plus.Communication.Nitro;
using Plus.Communication.RCON;
using Plus.Database;
using Plus.Plugins;
using Plus.Utilities.DependencyInjection;
using Scrutor;
using Plus.Core.FigureData;
using Plus.Core.Language;
using Plus.Core.Settings;
using Plus.HabboHotel;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Badges;
using Plus.HabboHotel.Bots;
using Plus.HabboHotel.Cache;
using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.Games;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Items.Televisions;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Navigator;
using Plus.HabboHotel.Permissions;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rewards;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Chat;
using Plus.HabboHotel.Subscriptions;
using Plus.HabboHotel.Talents;
using Plus.HabboHotel.Users.UserData;
using Plus.HabboHotel.Catalog.Utilities;
using Microsoft.Extensions.Options;

namespace Plus;

public static class Program
{
    private static readonly Dictionary<ServiceLifetime, IEnumerable<Type>> _defaultTypes = new();
    private static IServiceProvider? _serviceProvider;
    private static IRuntimeControlService? _runtimeControlService;
    private static IConsoleCommandHandler? _consoleCommandHandler;
    private static readonly string _bootLogPath = Path.Join(Directory.GetCurrentDirectory(), "boot.log");

    public static async Task Main(string[] args)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        Console.ForegroundColor = ConsoleColor.Gray;
        WriteBootstrapStatus("PlusEMU bootstrap starting...");
        
        var services = new ServiceCollection();
        WriteBootstrapStatus("Discovering default DI contracts...");
        _defaultTypes[ServiceLifetime.Singleton] = typeof(Program).Assembly.GetTypes().Where(t => t.IsInterface && t.GetCustomAttributes<SingletonAttribute>().Any());
        _defaultTypes[ServiceLifetime.Scoped] = typeof(Program).Assembly.GetTypes().Where(t => t.IsInterface && t.GetCustomAttributes<ScopedAttribute>().Any());

        // Plugins
        WriteBootstrapStatus("Loading plugins...");
        Directory.CreateDirectory("plugins");
        var pluginAssemblies = new DirectoryInfo("plugins").GetDirectories().Select(d => PluginLoadContext.LoadPlugin(Path.Join("plugins", d.Name), d.Name)).ToList();
        pluginAssemblies.AddRange(new DirectoryInfo("plugins").GetFiles().Where(f => Path.GetExtension(f.Name).Equals(".dll")).Select(f => PluginLoadContext.LoadPlugin(Path.Join("plugins"), Path.GetFileNameWithoutExtension(f.Name))));
        var pluginDefinitions = pluginAssemblies.SelectMany(pluginAssembly => AddPlugin(services, pluginAssembly)).ToList();

        // Configuration
        WriteBootstrapStatus("Loading configuration...");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Join(Directory.GetCurrentDirectory(), "Config"))
            .AddJsonFile("config.json")
            .Build();

        services.AddConfiguration<FlashServerConfiguration>(configuration.GetSection("Flash"));
        services.AddConfiguration<NitroServerConfiguration>(configuration.GetSection("Nitro"));
        services.AddConfiguration<DatabaseConfiguration>(configuration.GetSection("Database"));
        services.AddConfiguration<RconConfiguration>(configuration.GetSection("Rcon"));

        // Dependency Injection
        WriteBootstrapStatus("Registering DI rules...");
        services.AddDefaultRules(typeof(Program).Assembly);

        foreach (var plugin in pluginDefinitions)
            plugin.OnServicesConfigured();


        // Configuration
        LogManager.LoadConfiguration(Path.Join(Directory.GetCurrentDirectory(), "Config", "nlog.config"));
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            loggingBuilder.AddNLog();
        });

        WriteBootstrapStatus("Building service provider...");
        var serviceProvider = services.BuildServiceProvider();
        _serviceProvider = serviceProvider;
        WriteBootstrapStatus("Running plugin service-provider hooks...");
        foreach (var plugin in pluginDefinitions)
            plugin.OnServiceProviderBuild(serviceProvider);

        Console.ForegroundColor = ConsoleColor.White;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        ProbePlusEnvironmentDependencies(serviceProvider);
        WriteBootstrapStatus("Resolving Plus environment...");
        IPlusEnvironment environment;
        try
        {
            environment = serviceProvider.GetRequiredService<IPlusEnvironment>();
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteBootstrapStatus($"Failed to resolve IPlusEnvironment: {e}");
            return;
        }

        WriteBootstrapStatus("Starting Plus environment...");
        bool started;
        try
        {
            started = await environment.Start();
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteBootstrapStatus($"Environment startup crashed: {e}");
            return;
        }
        if (!started)
        {
            Environment.Exit(1);
            return;
        }
        Console.CursorVisible = false;
        while (true)
        {
            if (Console.ReadKey(true).Key == ConsoleKey.Enter)
            {
                Console.Write("plus> ");
                var input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    GetConsoleCommandHandler()?.InvokeCommand(input);
                }
            }
        }
    }

    public static IServiceCollection AddAssignableTo<T>(this IServiceCollection services, Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Singleton) => services.AddAssignableTo(new[] { assembly }, typeof(T), lifetime);

    public static IServiceCollection AddAssignableTo<T>(this IServiceCollection services, IEnumerable<Assembly> assemblies, ServiceLifetime lifetime = ServiceLifetime.Singleton) => services.AddAssignableTo(assemblies, typeof(T), lifetime);

    public static IServiceCollection AddAssignableTo(this IServiceCollection services, Assembly assembly, Type type, ServiceLifetime lifetime = ServiceLifetime.Singleton) => services.AddAssignableTo(new[] { assembly }, type, lifetime);

    public static IServiceCollection AddAssignableTo(this IServiceCollection services, IEnumerable<Assembly> assemblies, Type type, ServiceLifetime lifetime = ServiceLifetime.Singleton) =>
        services.Scan(scan => scan.FromAssemblies(assemblies)
            .AddClasses(classes => classes.Where(t => t.IsAssignableTo(type) && !t.IsAbstract && !t.IsInterface))
            .UsingRegistrationStrategy(RegistrationStrategy.Append)
            .AsSelfWithInterfaces()
            .WithSingletonLifetime());

    private static IServiceCollection AddDefaultRules(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(t => t.IsInterface && t.GetCustomAttributes<SingletonAttribute>().Any()).Concat(_defaultTypes[ServiceLifetime.Singleton]).Distinct())
            services.AddAssignableTo(assembly, type, ServiceLifetime.Singleton);
        foreach (var type in assembly.GetTypes().Where(t => t.IsInterface && t.GetCustomAttributes<ScopedAttribute>().Any()).Concat(_defaultTypes[ServiceLifetime.Scoped]).Distinct())
            services.AddAssignableTo(assembly, type, ServiceLifetime.Scoped);

        services.Scan(scan => scan.FromAssemblies(assembly)
            .AddClasses(classes => classes.Where(c => c.GetInterface($"I{c.Name}") != null))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsSelfWithInterfaces()
            .WithSingletonLifetime());
        return services;
    }

    private static IEnumerable<IPluginDefinition> AddPlugin(IServiceCollection services, Assembly pluginAssembly)
    {
        var pluginDefinitions = new List<IPluginDefinition>();
        try
        {
            services.AddDefaultRules(pluginAssembly);

            foreach (var pluginDefinition in pluginAssembly.DefinedTypes.Where(t =>
                         t.ImplementedInterfaces.Contains(typeof(IPluginDefinition))))
            {
                var plugin = (IPluginDefinition?)Activator.CreateInstance(pluginDefinition);
                if (plugin != null)
                {
                    plugin.ConfigureServices(services);
                    services.AddSingleton(plugin);
                    pluginDefinitions.Add(plugin);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load plugin assembly { pluginAssembly.FullName}. Possibly outdated. {e.Message}");
        }
        return pluginDefinitions;
    }
    public static IServiceCollection AddConfiguration<T>(this IServiceCollection services, IConfigurationSection section)
        where T : class
    {
        services.Configure<T>(section);
        return services;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        var logger = LogManager.GetLogger("Plus.Program");
        if (args.ExceptionObject is Exception e)
        {
            WriteBootstrapStatus($"Unhandled exception terminated the emulator: {e}");
            logger.Error(e, "Unhandled exception terminated the emulator.");
            GetRuntimeControlService()?.PerformShutdown($"Unhandled exception: {e.GetType().Name}: {e.Message}");
        }
        else
        {
            WriteBootstrapStatus($"Unhandled non-exception object terminated the emulator: {args.ExceptionObject}");
            logger.Error("Unhandled non-exception object terminated the emulator: {exceptionObject}", args.ExceptionObject);
            GetRuntimeControlService()?.PerformShutdown("Unhandled non-exception object");
        }
    }

    private static IRuntimeControlService? GetRuntimeControlService()
    {
        if (_runtimeControlService != null)
            return _runtimeControlService;

        try
        {
            _runtimeControlService = _serviceProvider?.GetService<IRuntimeControlService>();
        }
        catch (Exception e)
        {
            WriteBootstrapStatus($"Failed to resolve runtime control service lazily: {e}");
        }

        return _runtimeControlService;
    }

    private static IConsoleCommandHandler? GetConsoleCommandHandler()
    {
        if (_consoleCommandHandler != null)
            return _consoleCommandHandler;

        try
        {
            _consoleCommandHandler = _serviceProvider?.GetService<IConsoleCommandHandler>();
        }
        catch (Exception e)
        {
            WriteBootstrapStatus($"Failed to resolve console command handler lazily: {e}");
        }

        return _consoleCommandHandler;
    }

    private static void ProbePlusEnvironmentDependencies(IServiceProvider serviceProvider)
    {
        WriteBootstrapStatus("Probing PlusEnvironment dependency chain...");
        ProbeResolve<IDatabase>(serviceProvider, nameof(IDatabase));
        ProbeResolve<ILanguageManager>(serviceProvider, nameof(ILanguageManager));
        ProbeResolve<ISettingsManager>(serviceProvider, nameof(ISettingsManager));
        ProbeResolve<IFigureDataManager>(serviceProvider, nameof(IFigureDataManager));
        ProbeGameDependencies(serviceProvider);
        ProbeResolve<IGame>(serviceProvider, nameof(IGame));
        ProbeResolve<IServerRuntimeState>(serviceProvider, nameof(IServerRuntimeState));
        ProbeResolve<IEnumerable<IStartable>>(serviceProvider, "IEnumerable<IStartable>");
        ProbeResolve<IRconSocket>(serviceProvider, nameof(IRconSocket));
        ProbeResolve<IOptions<RconConfiguration>>(serviceProvider, "IOptions<RconConfiguration>");
        ProbeResolve<IOptions<FlashServerConfiguration>>(serviceProvider, "IOptions<FlashServerConfiguration>");
        ProbeResolve<IOptions<NitroServerConfiguration>>(serviceProvider, "IOptions<NitroServerConfiguration>");
        ProbeResolve<IItemDataManager>(serviceProvider, nameof(IItemDataManager));
        ProbeResolve<IFlashServer>(serviceProvider, nameof(IFlashServer));
        ProbeResolve<INitroServer>(serviceProvider, nameof(INitroServer));
    }

    private static void ProbeGameDependencies(IServiceProvider serviceProvider)
    {
        WriteBootstrapStatus("Probing IGame dependency chain...");
        ProbeResolve<IGameClientManager>(serviceProvider, nameof(IGameClientManager));
        ProbeResolve<IModerationManager>(serviceProvider, nameof(IModerationManager));
        ProbeResolve<IItemDataManager>(serviceProvider, nameof(IItemDataManager));
        ProbeResolve<ICatalogManager>(serviceProvider, nameof(ICatalogManager));
        ProbeResolve<ITelevisionManager>(serviceProvider, nameof(ITelevisionManager));
        ProbeResolve<INavigatorManager>(serviceProvider, nameof(INavigatorManager));
        ProbeRoomManagerDependencies(serviceProvider);
        ProbeResolve<IRoomManager>(serviceProvider, nameof(IRoomManager));
        ProbeResolve<IChatManager>(serviceProvider, nameof(IChatManager));
        ProbeResolve<IGroupManager>(serviceProvider, nameof(IGroupManager));
        ProbeResolve<IQuestManager>(serviceProvider, nameof(IQuestManager));
        ProbeResolve<IQuestService>(serviceProvider, nameof(IQuestService));
        ProbeResolve<ICatalogService>(serviceProvider, nameof(ICatalogService));
        ProbeResolve<ITargetedOfferManager>(serviceProvider, nameof(ITargetedOfferManager));
        ProbeResolve<ITargetedOfferService>(serviceProvider, nameof(ITargetedOfferService));
        ProbeResolve<IAchievementService>(serviceProvider, nameof(IAchievementService));
        ProbeResolve<IAchievementManager>(serviceProvider, nameof(IAchievementManager));
        ProbeResolve<ITalentTrackManager>(serviceProvider, nameof(ITalentTrackManager));
        ProbeResolve<IGameDataManager>(serviceProvider, nameof(IGameDataManager));
        ProbeResolve<IServerStatusUpdater>(serviceProvider, nameof(IServerStatusUpdater));
        ProbeResolve<IBotManager>(serviceProvider, nameof(IBotManager));
        ProbeResolve<ICacheManager>(serviceProvider, nameof(ICacheManager));
        ProbeResolve<IRewardManager>(serviceProvider, nameof(IRewardManager));
        ProbeResolve<IBadgeManager>(serviceProvider, nameof(IBadgeManager));
        ProbeResolve<ISubscriptionManager>(serviceProvider, nameof(ISubscriptionManager));
        ProbeResolve<IPermissionManager>(serviceProvider, nameof(IPermissionManager));
        ProbeResolve<IRoomService>(serviceProvider, nameof(IRoomService));
        ProbeResolve<IRoomFactory>(serviceProvider, nameof(IRoomFactory));
        ProbeResolve<IRoomAppender>(serviceProvider, nameof(IRoomAppender));
        ProbeResolve<IItemService>(serviceProvider, nameof(IItemService));
        ProbeResolve<IBotUtility>(serviceProvider, nameof(IBotUtility));
        ProbeResolve<IPetUtility>(serviceProvider, nameof(IPetUtility));
    }

    private static void ProbeRoomManagerDependencies(IServiceProvider serviceProvider)
    {
        WriteBootstrapStatus("Probing IRoomManager dependency chain...");
        ProbeResolve<IRoomFactory>(serviceProvider, nameof(IRoomFactory));
        ProbeResolve<IItemLoader>(serviceProvider, nameof(IItemLoader));
        ProbeResolve<IRoomItemPersistenceService>(serviceProvider, nameof(IRoomItemPersistenceService));
        ProbeResolve<IRoomItemPlacementValidatorService>(serviceProvider, nameof(IRoomItemPlacementValidatorService));
        ProbeResolve<IRoomItemPlacementPersistenceService>(serviceProvider, nameof(IRoomItemPlacementPersistenceService));
        ProbeResolve<IRoomRollerService>(serviceProvider, nameof(IRoomRollerService));
        ProbeResolve<IRoomItemInventoryService>(serviceProvider, nameof(IRoomItemInventoryService));
        ProbeResolve<IRoomItemUpdateQueueService>(serviceProvider, nameof(IRoomItemUpdateQueueService));
        ProbeResolve<IRoomItemLoadService>(serviceProvider, nameof(IRoomItemLoadService));
        ProbeResolve<IRoomItemRemovalService>(serviceProvider, nameof(IRoomItemRemovalService));
        ProbeResolve<IRoomItemStateService>(serviceProvider, nameof(IRoomItemStateService));
        ProbeResolve<IRoomItemPlacementApplyService>(serviceProvider, nameof(IRoomItemPlacementApplyService));
        ProbeResolve<IRoomItemTrackingService>(serviceProvider, nameof(IRoomItemTrackingService));
        ProbeResolve<IRoomRollerApplyService>(serviceProvider, nameof(IRoomRollerApplyService));
        ProbeResolve<IGroupManager>(serviceProvider, nameof(IGroupManager));
        ProbeResolve<IChatManager>(serviceProvider, nameof(IChatManager));
        ProbeResolve<IBotManager>(serviceProvider, nameof(IBotManager));
        ProbeResolve<IAchievementService>(serviceProvider, nameof(IAchievementService));
        ProbeResolve<IQuestService>(serviceProvider, nameof(IQuestService));
        ProbeResolve<ICacheManager>(serviceProvider, nameof(ICacheManager));
        ProbeResolve<ILanguageManager>(serviceProvider, nameof(ILanguageManager));
        ProbeResolve<IBadgeManager>(serviceProvider, nameof(IBadgeManager));
        ProbeResolve<IRoomDependencyResolver>(serviceProvider, nameof(IRoomDependencyResolver));
        ProbeResolve<IItemTeleporterFinder>(serviceProvider, nameof(IItemTeleporterFinder));
        ProbeResolve<IItemHopperFinder>(serviceProvider, nameof(IItemHopperFinder));
        ProbeResolve<IUserDataFactory>(serviceProvider, nameof(IUserDataFactory));
        ProbeResolve<IServerStatusSignal>(serviceProvider, nameof(IServerStatusSignal));
        ProbeResolve<ILoggerFactory>(serviceProvider, nameof(ILoggerFactory));
        ProbeResolve<ILogger<RoomManager>>(serviceProvider, "ILogger<RoomManager>");
    }

    private static void ProbeResolve<T>(IServiceProvider serviceProvider, string displayName)
        where T : notnull
    {
        WriteBootstrapStatus($"Resolving dependency: {displayName}...");
        try
        {
            _ = serviceProvider.GetRequiredService<T>();
            WriteBootstrapStatus($"Resolved dependency: {displayName}");
        }
        catch (Exception e)
        {
            WriteBootstrapStatus($"Failed dependency: {displayName}: {e}");
            throw;
        }
    }

    private static void WriteBootstrapStatus(string message)
    {
        var line = $"[boot] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        try
        {
            Console.WriteLine(line);
            Console.Out.Flush();
            Console.Error.WriteLine(line);
            Console.Error.Flush();
        }
        catch
        {
        }

        try
        {
            File.AppendAllText(_bootLogPath, line + Environment.NewLine);
        }
        catch
        {
        }
    }
}
