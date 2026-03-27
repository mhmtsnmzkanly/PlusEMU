using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;

namespace Plus.HabboHotel.Talents;

public class TalentTrackManager : ITalentTrackManager
{
    private readonly ILogger<TalentTrackManager> _logger;
    private readonly IDatabase _database;

    private readonly Dictionary<int, TalentTrackLevel> _citizenshipLevels;

    public TalentTrackManager(ILogger<TalentTrackManager> logger, IDatabase database)
    {
        _logger = logger;
        _database = database;
        _citizenshipLevels = new();
    }

    public void Init()
    {
        using var db = _database.Connection();
        var rows = db.Query("SELECT `type`, `level`, `data_actions`, `data_gifts` FROM `talents`");
        foreach (var row in rows)
        {
            _citizenshipLevels.Add(
                (int)row.level,
                new(
                    ((string?)row.type) ?? string.Empty,
                    (int)row.level,
                    ((string?)row.data_actions) ?? string.Empty,
                    ((string?)row.data_gifts) ?? string.Empty));
        }
        _logger.LogInformation("Loaded {Count} talent track levels", _citizenshipLevels.Count);
    }

    public ICollection<TalentTrackLevel> GetLevels() => _citizenshipLevels.Values;
}
