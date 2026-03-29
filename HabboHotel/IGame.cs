namespace Plus.HabboHotel;

public interface IGame
{
    void StartGameLoop();
    void StopGameLoop();
    Task Init();
}
