namespace Plus.HabboHotel.Items.Wired;

internal static class WiredConditionDataParser
{
    public static bool TryParseSingleValue(string stringData, out int value)
    {
        value = 0;
        return !string.IsNullOrWhiteSpace(stringData) && int.TryParse(stringData, out value);
    }

    public static bool TryParseUserCountRange(string stringData, out int minimum, out int maximum)
    {
        minimum = 0;
        maximum = 0;

        var parts = stringData.Split(';');
        return parts.Length >= 2 &&
               int.TryParse(parts[0], out minimum) &&
               int.TryParse(parts[1], out maximum);
    }

    public static bool TryParseStatePositionModes(string stringData, out int stateMode, out int directionMode, out int positionMode)
    {
        stateMode = 0;
        directionMode = 0;
        positionMode = 0;

        var parts = stringData.Split(';');
        return parts.Length >= 3 &&
               int.TryParse(parts[0], out stateMode) &&
               int.TryParse(parts[1], out directionMode) &&
               int.TryParse(parts[2], out positionMode);
    }
}
