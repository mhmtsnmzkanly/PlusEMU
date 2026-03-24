using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Moderation;

public interface IModerationRoomService
{
    Task ModerateRoom(GameClient session, uint roomId, bool setLock, bool setName, bool kickAll);
}
