using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator.Fun;

internal class SummonCommand : ITargetChatCommand
{
    private readonly IGameClientManager _gameClientManager;
    public string Key => "summon";
    public string PermissionRequired => "command_summon";

    public string Parameters => "%username%";

    public string Description => "Bring another user to your current room.";

    public bool MustBeInSameRoom => false;

    public SummonCommand(IGameClientManager gameClientManager)
    {
        _gameClientManager = gameClientManager;
    }

    public Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        var habbo = session.GetHabbo();
        var currentRoom = habbo?.CurrentRoom;
        var targetClient = target.Client;
        if (currentRoom == null)
            return Task.CompletedTask;

        if (habbo == null)
            return Task.CompletedTask;

        if (target.Username == habbo.Username)
        {
            session.SendWhisper("Get a life.");
            return Task.CompletedTask;
        }
        if (targetClient == null)
            return Task.CompletedTask;
        targetClient.SendNotification($"You have been summoned to {habbo.Username}!");
        if (!target.InRoom)
            targetClient.Send(new RoomForwardComposer(currentRoom.Id));
        else
            target.PrepareRoom(currentRoom.Id, "");
        return Task.CompletedTask;
    }
}
