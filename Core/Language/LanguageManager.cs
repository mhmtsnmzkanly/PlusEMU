using Dapper;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Plus.Database;

namespace Plus.Core.Language;

public class LanguageManager : ILanguageManager
{
    private readonly IDatabase _database;
    private readonly ILogger<LanguageManager> _logger;
    private Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex PlaceholderRegex = new(@"\{(?<name>[A-Za-z0-9_]+)\}", RegexOptions.Compiled);

    public LanguageManager(IDatabase database, ILogger<LanguageManager> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task Reload()
    {
        using var connection = _database.Connection();
        _values = (await connection.QueryAsync<(string, string)>("SELECT `key`, `value` FROM `server_locale`"))
            .ToDictionary(x => x.Item1, x => x.Item2, StringComparer.OrdinalIgnoreCase);
        _logger.LogInformation("Loaded " + _values.Count + " language locales.");
    }

    public bool TryGetString(string key, out string value) => _values.TryGetValue(key, out value!);

    public string GetOrDefault(string key, string fallback)
    {
        if (TryGetString(key, out var localizedValue))
            return localizedValue;

        _logger.LogWarning("Missing language locale for key {LocaleKey}", key);
        return fallback;
    }

    public string Require(string key) => GetOrDefault(key, key);

    public string Format(string key, params (string Key, string Value)[] placeholders) =>
        FormatOrDefault(key, key, placeholders);

    public string FormatOrDefault(string key, string fallback, params (string Key, string Value)[] placeholders)
    {
        var template = GetOrDefault(key, fallback);
        if (placeholders.Length == 0)
            return template;

        var placeholderMap = placeholders.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var templatePlaceholders = PlaceholderRegex.Matches(template)
            .Select(match => match.Groups["name"].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var missingPlaceholders = templatePlaceholders
            .Where(name => !placeholderMap.ContainsKey(name))
            .ToArray();
        if (missingPlaceholders.Length > 0)
            _logger.LogWarning("Locale {LocaleKey} missing placeholder values for: {PlaceholderNames}", key, string.Join(", ", missingPlaceholders));

        var extraPlaceholders = placeholderMap.Keys
            .Where(name => !templatePlaceholders.Contains(name, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        if (extraPlaceholders.Length > 0)
            _logger.LogWarning("Locale {LocaleKey} received unused placeholder values for: {PlaceholderNames}", key, string.Join(", ", extraPlaceholders));

        return PlaceholderRegex.Replace(template, match =>
        {
            var placeholderName = match.Groups["name"].Value;
            return placeholderMap.TryGetValue(placeholderName, out var replacement) ? replacement : match.Value;
        });
    }

}
