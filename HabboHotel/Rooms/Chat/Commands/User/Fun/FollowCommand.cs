using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User.Fun;

internal class FollowCommand : ITargetChatCommand
{
    private readonly IRoomService _roomService;
    public string Key => "follow";
    public string PermissionRequired => "command_follow";

    public string Parameters => "%username%";

    public string Description => "Want to visit a specific user? Use this command!";

    public bool MustBeInSameRoom => false;

    public FollowCommand(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public async Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        if (session.GetHabbo() is not { Permissions: { } permissions } habbo)
            return;

        if (target.IsInRoom(room))
        {
            session.SendWhisper($"Hey you, open your eyes! {target.Username} is in this room!");
            return;
        }

        if (target.Username == habbo.Username)
        {
            session.SendWhisper("* Windows shutdown noise *");
            return;
        }

        if (!target.TryGetCurrentRoom(out var targetRoom))
        {
            session.SendWhisper("That user currently isn't in a room!");
            return;
        }

        if (targetRoom.Access != RoomAccess.Open && !(permissions?.HasRight("mod_tool") ?? false))
        {
            session.SendWhisper("Oops, the room that user is either locked, passworded or invisible. You cannot follow!");
            return;
        }

        await _roomService.PrepareRoom(session, targetRoom.RoomId, "");
    }
}
