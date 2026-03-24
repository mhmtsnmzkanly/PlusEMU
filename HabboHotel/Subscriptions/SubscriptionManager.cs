using System.Data;
using Microsoft.Extensions.Logging;
using Plus.Database;

namespace Plus.HabboHotel.Subscriptions;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly ILogger<SubscriptionManager> _logger;
    private readonly IDatabase _database;
    private readonly Dictionary<int, SubscriptionData> _subscriptions = new();

    public SubscriptionManager(ILogger<SubscriptionManager> logger, IDatabase database)
    {
        _logger = logger;
        _database = database;
    }

    public void Init()
    {
        if (_subscriptions.Count > 0)
            _subscriptions.Clear();
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery("SELECT * FROM `subscriptions`;");
            var getSubscriptions = dbClient.GetTable();
            if (getSubscriptions != null)
            {
                foreach (DataRow row in getSubscriptions.Rows)
                {
                    if (!_subscriptions.ContainsKey(Convert.ToInt32(row["id"])))
                    {
                        _subscriptions.Add(Convert.ToInt32(row["id"]),
                            new(Convert.ToInt32(row["id"]), Convert.ToString(row["name"]) ?? string.Empty, Convert.ToString(row["badge_code"]) ?? string.Empty, Convert.ToInt32(row["credits"]),
                                Convert.ToInt32(row["duckets"]), Convert.ToInt32(row["respects"])));
                    }
                }
            }
        }
        _logger.LogInformation("Loaded " + _subscriptions.Count + " subscriptions.");
    }

    public bool TryGetSubscriptionData(int id, out SubscriptionData data) => _subscriptions.TryGetValue(id, out data);
}
