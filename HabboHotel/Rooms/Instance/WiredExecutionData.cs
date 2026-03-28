using Plus.HabboHotel.Items.Wired;

namespace Plus.HabboHotel.Rooms.Instance;

internal sealed class WiredExecutionData
{
    public WiredExecutionData(WiredBoxType type, params object[] parameters)
    {
        Type = type;
        Parameters = parameters;
    }

    public WiredBoxType Type { get; }
    public object[] Parameters { get; }
}
