using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands;

public interface IChatCommand : ICommandBase
{
    Task Execute(GameClient session, Room room, string[] parameters);
}