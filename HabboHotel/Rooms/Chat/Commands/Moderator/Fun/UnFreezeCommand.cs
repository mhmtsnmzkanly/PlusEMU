using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator.Fun;

internal class UnFreezeCommand : ITargetChatCommand
{
    private readonly IGameClientManager _gameClientManager;
    public string Key => "unfreeze";
    public string PermissionRequired => "command_unfreeze";

    public string Parameters => "%username%";

    public string Description => "Allow another user to walk again.";

    public bool MustBeInSameRoom => true;

    public UnFreezeCommand(IGameClientManager gameClientManager)
    {
        _gameClientManager = gameClientManager;
    }

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.IsInRoom(room))
            return Task.CompletedTask;

        var targetUser = room.GetRoomUserManager().GetRoomUserByHabbo(target.Id);
        if (targetUser != null)
            targetUser.Frozen = false;
        session.SendWhisper($"Successfully unfroze {target.Username}!");
        return Task.CompletedTask;
    }
}
