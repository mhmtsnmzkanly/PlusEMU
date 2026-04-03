namespace Plus.HabboHotel.Users;

public interface IHabboRuntimeInitializer
{
    void EnsureVisualComponents(Habbo habbo);
    void EnsureProcessComponent(Habbo habbo);
}
