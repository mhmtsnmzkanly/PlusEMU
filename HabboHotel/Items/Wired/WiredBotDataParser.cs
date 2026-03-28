namespace Plus.HabboHotel.Items.Wired;

internal static class WiredBotDataParser
{
    public static bool TryParseBotName(string? data, out string botName)
    {
        botName = string.Empty;

        return !string.IsNullOrWhiteSpace(data) &&
               (botName = data.Trim()) != null;
    }

    public static bool TryParseBotClothing(string? data, out string botName, out string figure)
    {
        botName = string.Empty;
        figure = string.Empty;

        if (string.IsNullOrWhiteSpace(data))
            return false;

        var parts = data.Split('\t');
        return parts.Length == 2 &&
               !string.IsNullOrWhiteSpace(parts[0]) &&
               !string.IsNullOrWhiteSpace(parts[1]) &&
               (botName = parts[0]) != null &&
               (figure = parts[1]) != null;
    }
}
