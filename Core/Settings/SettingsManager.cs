using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;

namespace Plus.Core.Settings;

public class SettingsManager : ISettingsManager
{
    private readonly IDatabase _database;
    private readonly ILogger<SettingsManager> _logger;
    private Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlyList<SettingsKeyDefinition> Definitions =
    [
        new("catalog.enabled", SettingsValueType.Bool, Owner: "Catalog"),
        new("hotel.targetoffer.id", SettingsValueType.Int, Min: 0, Owner: "Catalog"),
        new("catalog.group.purchase.cost", SettingsValueType.Int, Min: 0, Owner: "Groups"),
        new("group.delete.member.limit", SettingsValueType.Int, Min: 0, Owner: "Groups"),
        new("messenger.buddy_limit", SettingsValueType.Int, Min: 0, Owner: "Messenger"),
        new("room.chat.filter.banned_phrases.chances", SettingsValueType.Int, Min: 0, Owner: "Chat"),
        new("room.item.exchangeables.enabled", SettingsValueType.Bool, Owner: "Items"),
        new("room.item.gifts.enabled", SettingsValueType.Bool, Owner: "Catalog"),
        new("room.item.placement_limit", SettingsValueType.Int, Min: 1, Owner: "Items"),
        new("room.pets.placement_limit", SettingsValueType.Int, Min: 0, Owner: "Rooms"),
        new("room.promotion.lifespan", SettingsValueType.Int, Min: 0, Owner: "Rooms"),
        new("trading.auto_exchange_redeemables", SettingsValueType.Bool, Owner: "Trading"),
        new("user.currency_scheduler.credit_reward", SettingsValueType.Int, Min: 0, Owner: "Users"),
        new("user.currency_scheduler.ducket_reward", SettingsValueType.Int, Min: 0, Owner: "Users"),
        new("user.currency_scheduler.tick", SettingsValueType.Int, Min: 1, Owner: "Users"),
        new("user.login.message.enabled", SettingsValueType.Bool, Owner: "Authentication")
    ];
    private static readonly Dictionary<string, SettingsKeyDefinition> DefinitionsByKey =
        Definitions.ToDictionary(definition => definition.Key, StringComparer.OrdinalIgnoreCase);

    public SettingsManager(IDatabase database, ILogger<SettingsManager> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task Reload()
    {
        using var connection = _database.Connection();
        _settings = (await connection.QueryAsync<(string, string)>("SELECT `key`, `value` FROM `server_settings`"))
            .ToDictionary(x => x.Item1, x => x.Item2, StringComparer.OrdinalIgnoreCase);
        ValidateLoadedSettings();
        _logger.LogInformation("Loaded " + _settings.Count + " server settings.");
    }

    public bool TryGetString(string key, out string value) => _settings.TryGetValue(key, out value!);

    public string GetStringOrDefault(string key, string defaultValue) =>
        TryGetString(key, out var value) ? value : defaultValue;

    public int GetIntOrDefault(string key, int defaultValue)
    {
        if (!TryGetString(key, out var rawValue))
            return defaultValue;

        if (int.TryParse(rawValue, out var parsedValue))
            return parsedValue;

        _logger.LogWarning("Setting {SettingKey} expected int but received {SettingValue}. Using default {DefaultValue}.", key, rawValue, defaultValue);
        return defaultValue;
    }

    public bool GetBoolOrDefault(string key, bool defaultValue)
    {
        if (!TryGetString(key, out var rawValue))
            return defaultValue;

        if (TryParseBool(rawValue, out var parsedValue))
            return parsedValue;

        _logger.LogWarning("Setting {SettingKey} expected bool but received {SettingValue}. Using default {DefaultValue}.", key, rawValue, defaultValue);
        return defaultValue;
    }

    public int RequireInt(string key, int? min = null, int? max = null)
    {
        if (!TryGetString(key, out var rawValue))
            throw new InvalidOperationException($"Missing required int setting: {key}");

        if (!int.TryParse(rawValue, out var parsedValue))
            throw new InvalidOperationException($"Setting {key} must be a valid int.");

        if (min.HasValue && parsedValue < min.Value)
            throw new InvalidOperationException($"Setting {key} must be >= {min.Value}.");
        if (max.HasValue && parsedValue > max.Value)
            throw new InvalidOperationException($"Setting {key} must be <= {max.Value}.");

        return parsedValue;
    }

    public bool RequireBool(string key)
    {
        if (!TryGetString(key, out var rawValue))
            throw new InvalidOperationException($"Missing required bool setting: {key}");

        if (TryParseBool(rawValue, out var parsedValue))
            return parsedValue;

        throw new InvalidOperationException($"Setting {key} must be a valid bool.");
    }

    private void ValidateLoadedSettings()
    {
        var unusedKeys = _settings.Keys
            .Where(key => !DefinitionsByKey.ContainsKey(key))
            .OrderBy(key => key)
            .ToArray();

        var missingKeys = Definitions
            .Where(definition => definition.Required && !_settings.ContainsKey(definition.Key))
            .Select(definition => definition.Key)
            .OrderBy(key => key)
            .ToArray();

        var invalidKeys = new List<string>();
        foreach (var definition in Definitions)
        {
            if (!_settings.TryGetValue(definition.Key, out var value))
                continue;

            switch (definition.Type)
            {
                case SettingsValueType.Bool:
                    if (!TryParseBool(value, out _))
                        invalidKeys.Add($"{definition.Key}=bool({value})");
                    break;
                case SettingsValueType.Int:
                    if (!int.TryParse(value, out var parsedInt))
                    {
                        invalidKeys.Add($"{definition.Key}=int({value})");
                        break;
                    }

                    if (definition.Min.HasValue && parsedInt < definition.Min.Value)
                        invalidKeys.Add($"{definition.Key}=min({parsedInt}<{definition.Min.Value})");
                    if (definition.Max.HasValue && parsedInt > definition.Max.Value)
                        invalidKeys.Add($"{definition.Key}=max({parsedInt}>{definition.Max.Value})");
                    break;
            }
        }

        if (unusedKeys.Length > 0)
            _logger.LogWarning("Unused server settings keys detected: {UnusedKeys}", string.Join(", ", unusedKeys));
        if (missingKeys.Length > 0)
            _logger.LogError("Missing required server settings keys detected: {MissingKeys}", string.Join(", ", missingKeys));
        if (invalidKeys.Count > 0)
            _logger.LogError("Invalid server settings detected: {InvalidKeys}", string.Join(", ", invalidKeys));
    }

    private static bool TryParseBool(string value, out bool parsedValue)
    {
        if (bool.TryParse(value, out parsedValue))
            return true;

        switch (value.Trim())
        {
            case "1":
                parsedValue = true;
                return true;
            case "0":
                parsedValue = false;
                return true;
            default:
                parsedValue = false;
                return false;
        }
    }
}
