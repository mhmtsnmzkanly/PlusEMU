using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator.Fun;

internal class SummonCommand : ITargetChatCommand
{
    private readonly IRoomService _roomService;
    public string Key => "summon";
    public string PermissionRequired => "command_summon";

    public string Parameters => "%username%";

    public string Description => "Bring another user to your current room.";

    public bool MustBeInSameRoom => false;

    public SummonCommand(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public async Task Execute(GameClient session, Room room, Habbo target, string[] parameters)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !target.TryGetClient(out var targetClient))
            return;

        if (target.Username == habbo.Username)
        {
            session.SendWhisper("Get a life.");
            return;
        }

        targetClient.SendNotification($"You have been summoned to {habbo.Username}!");
        
        if (!target.TryGetCurrentRoom(out _))
        {
            targetClient.Send(new RoomForwardComposer(room.Id));
        }
        else
        {
            await _roomService.PrepareRoom(targetClient, room.Id, "");
        }
    }
}
