namespace Plus.HabboHotel.Items.Wired;

internal static class WiredFurniSnapshotParser
{
    public static bool TryParseEntry(string rawData, out uint itemId, out WiredFurniSnapshot snapshot)
    {
        itemId = 0;
        snapshot = default;

        var parts = rawData.Split(':');
        if (parts.Length < 2 ||
            string.IsNullOrEmpty(parts[0]) ||
            string.IsNullOrEmpty(parts[1]) ||
            !uint.TryParse(parts[0], out itemId))
            return false;

        var values = parts[1].Split(',');
        if (values.Length < 4 ||
            !int.TryParse(values[0], out var x) ||
            !int.TryParse(values[1], out var y) ||
            !double.TryParse(values[2], out var z) ||
            !int.TryParse(values[3], out var rotation))
            return false;

        snapshot = new WiredFurniSnapshot(x, y, z, rotation, values.Length >= 5 ? values[4] : "1");
        return true;
    }
}

internal readonly record struct WiredFurniSnapshot(int X, int Y, double Z, int Rotation, string State);
