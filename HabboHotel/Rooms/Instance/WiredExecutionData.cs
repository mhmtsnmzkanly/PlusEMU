using Plus.HabboHotel.Items.Wired;

namespace Plus.HabboHotel.Rooms.Instance;

internal sealed class WiredExecutionData
{
    public WiredExecutionData(WiredBoxType type, IReadOnlyCollection<uint>? targetItemIds = null, params object[] parameters)
    {
        Type = type;
        TargetItemIds = targetItemIds;
        Parameters = parameters;
    }

    public WiredBoxType Type { get; }
    public IReadOnlyCollection<uint>? TargetItemIds { get; }
    public object[] Parameters { get; }
}
