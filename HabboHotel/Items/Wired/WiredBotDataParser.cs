namespace Plus.HabboHotel.Items.Wired;

internal static class WiredBotDataParser
{
    public static bool TryParseBotName(string? data, out string botName)
    {
        botName = string.Empty;

        if (string.IsNullOrWhiteSpace(data))
            return false;

        botName = data.Trim();
        return botName.Length > 0;
    }

    public static bool TryParseBotClothing(string? data, out string botName, out string figure)
    {
        botName = string.Empty;
        figure = string.Empty;

        if (string.IsNullOrWhiteSpace(data))
            return false;

        var parts = data.Split('\t');
        if (parts.Length != 2 ||
            string.IsNullOrWhiteSpace(parts[0]) ||
            string.IsNullOrWhiteSpace(parts[1]))
            return false;

        botName = parts[0];
        figure = parts[1];
        return true;
    }

    public static bool TryParseBotCommunication(string? data, out string botName, out string message, out int chatMode)
    {
        botName = string.Empty;
        message = string.Empty;
        chatMode = 0;

        if (string.IsNullOrWhiteSpace(data))
            return false;

        var lastSeparator = data.LastIndexOf(';');
        if (lastSeparator <= 0 || lastSeparator >= data.Length - 1)
            return false;

        if (!int.TryParse(data[(lastSeparator + 1)..], out chatMode))
            return false;

        var config = data[..lastSeparator];
        var tabParts = config.Split('\t');
        if (tabParts.Length == 2 &&
            !string.IsNullOrWhiteSpace(tabParts[0]) &&
            !string.IsNullOrWhiteSpace(tabParts[1]))
        {
            botName = tabParts[0];
            message = tabParts[1];
            return true;
        }

        var separator = config.IndexOf(';');
        if (separator <= 0 || separator >= config.Length - 1)
            return false;

        botName = config[..separator];
        message = config[(separator + 1)..];
        return !string.IsNullOrWhiteSpace(botName) && !string.IsNullOrWhiteSpace(message);
    }
}
