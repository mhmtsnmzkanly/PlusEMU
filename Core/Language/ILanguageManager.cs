namespace Plus.Core.Language;

public interface ILanguageManager
{
    bool TryGetString(string key, out string value);
    string GetOrDefault(string key, string fallback);
    string Require(string key);
    string Format(string key, params (string Key, string Value)[] placeholders);
    string FormatOrDefault(string key, string fallback, params (string Key, string Value)[] placeholders);
    Task Reload();
}
