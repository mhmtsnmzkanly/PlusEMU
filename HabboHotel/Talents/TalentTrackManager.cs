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
        var levels = db.Query("SELECT `type`, `level`, `data_actions`, `data_gifts` FROM `talents`").ToList();
        var subLevels = db.Query("SELECT `talent_level`, `sub_level` AS Level, `badge_code` AS Badge, `required_progress` AS RequiredProgress FROM `talents_sub_levels`")
                       .GroupBy(s => (int)s.talent_level)
                       .ToDictionary(g => g.Key, g => g.ToList());

        _citizenshipLevels.Clear();
        foreach (var row in levels)
        {
            var levelId = (int)row.level;
            
            var currentSubLevels = new List<TalentTrackSubLevel>();
            if (subLevels.TryGetValue(levelId, out var subLevelList))
            {
                currentSubLevels.AddRange(subLevelList.Select(s => new TalentTrackSubLevel((int)s.Level, (string)s.Badge ?? string.Empty, (int)s.RequiredProgress)));
            }
            
            _citizenshipLevels.Add(levelId, new TalentTrackLevel(
                ((string?)row.type) ?? string.Empty,
                levelId,
                ((string?)row.data_actions) ?? string.Empty,
                ((string?)row.data_gifts) ?? string.Empty,
                currentSubLevels));
        }
        _logger.LogInformation("Loaded {Count} talent track levels", _citizenshipLevels.Count);
    }

    public ICollection<TalentTrackLevel> GetLevels() => _citizenshipLevels.Values;
}
