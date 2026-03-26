using System;
using System.Collections.Generic;
using Dapper;
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

        using var connection = _database.Connection();
        var subscriptions = connection.Query("SELECT * FROM `subscriptions`;");

        foreach (var row in subscriptions)
        {
            var id = Convert.ToInt32(row.id);
            if (!_subscriptions.ContainsKey(id))
            {
                _subscriptions.Add(id,
                    new SubscriptionData(id, Convert.ToString(row.name) ?? string.Empty, Convert.ToString(row.badge_code) ?? string.Empty, Convert.ToInt32(row.credits),
                        Convert.ToInt32(row.duckets), Convert.ToInt32(row.respects)));
            }
        }

        _logger.LogInformation("Loaded " + _subscriptions.Count + " subscriptions.");
    }

    public bool TryGetSubscriptionData(int id, out SubscriptionData? data) => _subscriptions.TryGetValue(id, out data);
}
