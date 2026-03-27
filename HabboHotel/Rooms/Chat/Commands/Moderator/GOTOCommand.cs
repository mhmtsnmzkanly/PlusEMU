using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class GotoCommand : IChatCommand
{
    private readonly IRoomService _roomService;
    private readonly IRoomFactory _roomFactory;
    
    public string Key => "goto";
    public string PermissionRequired => "command_goto";

    public string Parameters => "%room_id%";

    public string Description => "Teleport to a room by its ID.";

    public GotoCommand(IRoomService roomService, IRoomFactory roomFactory)
    {
        _roomService = roomService;
        _roomFactory = roomFactory;
    }

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        if (!parameters.Any())
        {
            session.SendWhisper("You must specify a room id!");
            return;
        }

        if (!uint.TryParse(parameters[0], out var roomId))
        {
            session.SendWhisper("You must enter a valid room ID");
            return;
        }

        if (!_roomFactory.TryGetData(roomId, out _))
        {
            session.SendWhisper("This room does not exist!");
            return;
        }

        await _roomService.PrepareRoom(session, roomId, "");
    }
}
