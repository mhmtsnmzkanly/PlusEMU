using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator;

internal class KickCommand : ITargetChatCommand
{
    private readonly IRoomService _roomService;

    public KickCommand(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public string Key => "kick";
    public string PermissionRequired => "command_kick";

    public string Parameters => "%username% %reason%";

    public string Description => "Kick a user from a room and send them a reason.";

    public bool MustBeInSameRoom => false;
    
    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        var targetClient = target.Client;
        var targetRoom = target.CurrentRoom;
        if (target == session.GetHabbo())
        {
            session.SendWhisper("Get a life.");
            return Task.CompletedTask;
        }
        if (!target.InRoom || targetClient == null || targetRoom == null)
        {
            session.SendWhisper("That user currently isn't in a room.");
            return Task.CompletedTask;
        }
        if (parameters.Any())
            targetClient.SendNotification($"A moderator has kicked you from the room for the following reason: {CommandManager.MergeParams(parameters)}");
        else
            targetClient.SendNotification("A moderator has kicked you from the room.");
        return _roomService.LeaveRoom(targetClient);
    }
}
