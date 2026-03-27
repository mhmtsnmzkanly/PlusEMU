using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class UnloadCommand : IChatCommand
{
    private readonly IRoomManager _roomManager;
    public string Key => "unload";
    public string PermissionRequired => "command_unload";

    public string Parameters => "%id%";

    public string Description => "Unload the current room.";

    public UnloadCommand(IRoomManager roomManager)
    {
        _roomManager = roomManager;
    }
    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (room.CheckRights(session, true) || (habbo?.Permissions?.HasRight("room_unload_any") ?? false))
            _roomManager.UnloadRoom(room.Id);
    }
}
