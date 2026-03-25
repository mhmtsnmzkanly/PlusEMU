using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;
using Plus.Utilities;

namespace Plus.HabboHotel.Games;

public class GameDataManager : IGameDataManager
{
    private sealed class GameDataRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string ColourOne { get; init; } = string.Empty;
        public string ColourTwo { get; init; } = string.Empty;
        public string ResourcePath { get; init; } = string.Empty;
        public string StringThree { get; init; } = string.Empty;
        public string GameSwf { get; init; } = string.Empty;
        public string GameAssets { get; init; } = string.Empty;
        public string GameServerHost { get; init; } = string.Empty;
        public string GameServerPort { get; init; } = string.Empty;
        public string SocketPolicyPort { get; init; } = string.Empty;
        public string GameEnabled { get; init; } = "0";
    }

    private readonly IDatabase _database;
    private readonly ILogger<GameDataManager> _logger;

    private readonly Dictionary<int, GameData> _games;

    public GameDataManager(IDatabase database, ILogger<GameDataManager> logger)
    {
        _database = database;
        _logger = logger;
        _games = new();
    }

    public ICollection<GameData> GameData => _games.Values;

    public void Init()
    {
        if (_games.Count > 0)
            _games.Clear();
        using (var connection = _database.Connection())
        {
            foreach (var row in connection.Query<GameDataRow>(
                         """
                         SELECT
                             `id` AS Id,
                             `name` AS Name,
                             `colour_one` AS ColourOne,
                             `colour_two` AS ColourTwo,
                             `resource_path` AS ResourcePath,
                             `string_three` AS StringThree,
                             `game_swf` AS GameSwf,
                             `game_assets` AS GameAssets,
                             `game_server_host` AS GameServerHost,
                             `game_server_port` AS GameServerPort,
                             `socket_policy_port` AS SocketPolicyPort,
                             `game_enabled` AS GameEnabled
                         FROM `games_config`
                         """))
            {
                _games.Add(row.Id,
                    new(row.Id, row.Name, row.ColourOne, row.ColourTwo,
                        row.ResourcePath, row.StringThree, row.GameSwf, row.GameAssets,
                        row.GameServerHost, row.GameServerPort, row.SocketPolicyPort,
                        ConvertExtensions.EnumToBool(row.GameEnabled)));
            }
        }
        _logger.LogInformation("Game Data Manager -> LOADED");
    }

    public bool TryGetGame(int gameId, out GameData? data) => _games.TryGetValue(gameId, out data);

    public int GetCount()
    {
        return _games.Values.Count(x => x.Enabled);
    }
}
