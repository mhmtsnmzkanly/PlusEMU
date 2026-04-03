using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator.Fun;

internal class CoordsCommand : IChatCommand
{
    public string Key => "coords";
    public string PermissionRequired => "command_coords";

    public string Parameters => "";

    public string Description => "Used to get your current position within the room.";

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var currentRoom))
            return;

        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var thisUser) || thisUser == null)
            return;
        session.SendNotification(
            $"X: {thisUser.X}\n - Y: {thisUser.Y}\n - Z: {thisUser.Z}\n - Rot: {thisUser.RotBody}, sqState: {room.GetGameMap().GameMap[thisUser.X, thisUser.Y]}\n\n - RoomID: {currentRoom.RoomId}");
    }
}
