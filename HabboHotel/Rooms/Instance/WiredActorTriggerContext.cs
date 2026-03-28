using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Instance;

internal sealed class WiredActorTriggerContext
{
    public WiredActorTriggerContext(Habbo actor)
    {
        Actor = actor;
    }

    public Habbo Actor { get; }
}
