namespace Plus.Core.Settings;

public interface ISettingsManager
{
    bool TryGetString(string key, out string value);
    string GetStringOrDefault(string key, string defaultValue);
    int GetIntOrDefault(string key, int defaultValue);
    bool GetBoolOrDefault(string key, bool defaultValue);
    int RequireInt(string key, int? min = null, int? max = null);
    bool RequireBool(string key);
    Task Reload();
}
