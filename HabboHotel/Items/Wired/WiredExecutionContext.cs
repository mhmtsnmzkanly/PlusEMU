using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired;

public abstract class WiredExecutionContext
{
    protected WiredExecutionContext()
    {
    }

    public Habbo? Actor { get; protected init; }
    public Item? Item { get; protected init; }
    public string? Message { get; protected init; }
}
