using System.Diagnostics.CodeAnalysis;
using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;
using Plus.Utilities;

namespace Plus.HabboHotel.Items.Televisions;

public class TelevisionManager : ITelevisionManager
{
    private readonly ILogger<TelevisionManager> _logger;
    private readonly IDatabase _database;

    public TelevisionManager(ILogger<TelevisionManager> logger, IDatabase database)
    {
        _logger = logger;
        _database = database;
    }

    public Dictionary<int, TelevisionItem> Televisions { get; } = new();

    public ICollection<TelevisionItem> TelevisionList => Televisions.Values;

    public void Init()
    {
        if (Televisions.Count > 0)
            Televisions.Clear();
        using var db = _database.Connection();
        var rows = db.Query("SELECT `id`, `youtube_id`, `title`, `description`, `enabled` FROM `items_youtube` ORDER BY `id` DESC");
        foreach (var row in rows)
        {
            Televisions.Add((int)row.id,
                new(
                    (int)row.id,
                    ((string?)row.youtube_id) ?? string.Empty,
                    ((string?)row.title) ?? string.Empty,
                    ((string?)row.description) ?? string.Empty,
                    ConvertExtensions.EnumToBool(((string?)row.enabled) ?? "0")));
        }
        _logger.LogInformation("Television Items -> LOADED");
    }

    public bool TryGet(int itemId, [NotNullWhen(true)] out TelevisionItem? televisionItem)
    {
        if (Televisions.TryGetValue(itemId, out televisionItem))
            return true;
        return false;
    }
}
