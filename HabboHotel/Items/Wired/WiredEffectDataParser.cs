namespace Plus.HabboHotel.Items.Wired;

internal static class WiredEffectDataParser
{
    public static bool TryParseBotHandItem(string? data, out string botName, out int drinkId)
    {
        botName = string.Empty;
        drinkId = 0;

        if (string.IsNullOrWhiteSpace(data))
            return false;

        var parts = data.Split(';');
        if (parts.Length != 2 ||
            string.IsNullOrWhiteSpace(parts[0]) ||
            !int.TryParse(parts[1], out drinkId))
            return false;

        botName = parts[0];
        return true;
    }

    public static bool TryParseBotFollow(string? data, out int followMode, out string botName)
    {
        followMode = 0;
        botName = string.Empty;

        if (string.IsNullOrWhiteSpace(data))
            return false;

        var parts = data.Split(';');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out followMode) ||
            string.IsNullOrWhiteSpace(parts[1]))
            return false;

        botName = parts[1];
        return true;
    }

    public static bool TryParseMoveAndRotateModes(string? data, out int movementMode, out int rotationMode)
    {
        movementMode = 0;
        rotationMode = 0;

        if (string.IsNullOrWhiteSpace(data))
            return false;

        var parts = data.Split(';');
        return parts.Length == 2 &&
               int.TryParse(parts[0], out movementMode) &&
               int.TryParse(parts[1], out rotationMode);
    }

    public static bool TryParseMute(string? data, out int minutes, out string message)
    {
        minutes = 0;
        message = "No message!";

        if (string.IsNullOrWhiteSpace(data))
            return false;

        var separatorIndex = data.IndexOf(';');
        if (separatorIndex <= 0 || separatorIndex >= data.Length - 1)
            return false;

        return int.TryParse(data[..separatorIndex], out minutes) &&
               (message = data[(separatorIndex + 1)..]) != null;
    }
}
