using Plus.HabboHotel.Items.Wired;

namespace Plus.HabboHotel.Rooms.Instance;

internal sealed class WiredExecutionData
{
    public WiredExecutionData(WiredBoxType type, IReadOnlyCollection<uint>? targetItemIds = null, object? context = null)
    {
        Type = type;
        TargetItemIds = targetItemIds;
        Context = context;
    }

    public WiredBoxType Type { get; }
    public IReadOnlyCollection<uint>? TargetItemIds { get; }
    public object? Context { get; }
}
