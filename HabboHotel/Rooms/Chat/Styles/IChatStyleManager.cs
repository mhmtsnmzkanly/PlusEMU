namespace Plus.HabboHotel.Rooms.Chat.Styles;

public interface IChatStyleManager
{
    void Init();
    bool TryGetStyle(int id, out ChatStyle? style);
}
