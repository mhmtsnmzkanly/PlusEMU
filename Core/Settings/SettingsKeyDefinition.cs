namespace Plus.Core.Settings;

internal sealed record SettingsKeyDefinition(
    string Key,
    SettingsValueType Type,
    bool Required = true,
    string? DefaultValue = null,
    int? Min = null,
    int? Max = null,
    string? Owner = null);

internal enum SettingsValueType
{
    String,
    Int,
    Bool
}
