using System.Data;
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
        DataTable? data = null;
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery("SELECT `type`,`level`,`data_actions`,`data_gifts` FROM `talents`");
            data = dbClient.GetTable();
        }
        if (data != null)
        {
            foreach (DataRow row in data.Rows)
            {
                _citizenshipLevels.Add(Convert.ToInt32(row["level"]),
                    new(Convert.ToString(row["type"]) ?? string.Empty, Convert.ToInt32(row["level"]), Convert.ToString(row["data_actions"]) ?? string.Empty, Convert.ToString(row["data_gifts"]) ?? string.Empty));
            }
        }
        _logger.LogInformation("Loaded " + _citizenshipLevels.Count + " talent track levels");
    }

    public ICollection<TalentTrackLevel> GetLevels() => _citizenshipLevels.Values;
}
