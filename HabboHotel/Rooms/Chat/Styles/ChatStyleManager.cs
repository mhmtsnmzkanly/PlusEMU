using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;

namespace Plus.HabboHotel.Rooms.Chat.Styles;

public sealed class ChatStyleManager : IChatStyleManager
{
    private readonly ILogger<ChatStyleManager> _logger;
    private readonly IDatabase _database;

    private readonly Dictionary<int, ChatStyle> _styles;

    public ChatStyleManager(ILogger<ChatStyleManager> logger, IDatabase database)
    {
        _logger = logger;
        _database = database;
        _styles = new();
    }

    public void Init()
    {
        if (_styles.Count > 0)
            _styles.Clear();
        using var db = _database.Connection();
        var rows = db.Query("SELECT `id`, `name`, `required_right` FROM `room_chat_styles`");
        foreach (var row in rows)
        {
            try
            {
                int id = (int)row.id;
                if (!_styles.ContainsKey(id))
                    _styles.Add(id, new(id, ((string?)row.name) ?? string.Empty, ((string?)row.required_right) ?? string.Empty));
            }
            catch (Exception ex)
            {
                int safeId = 0;
                try { safeId = (int)row.id; } catch { /* ignored */ }
                _logger.LogError(ex, "Unable to load ChatBubble for ID [{Id}]", safeId);
            }
        }
        _logger.LogInformation("Loaded {Count} chat styles.", _styles.Count);
    }

    public bool TryGetStyle(int id, out ChatStyle? style) => _styles.TryGetValue(id, out style);
}
