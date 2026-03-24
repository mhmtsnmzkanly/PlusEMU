using Plus.Communication.Packets.Outgoing.Groups;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class RemoveGroupFavouriteEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null)
            return Task.CompletedTask;

        habbo.HabboStats.FavouriteGroupId = 0;
        var currentRoom = habbo.CurrentRoom;
        if (habbo.InRoom && currentRoom != null)
        {
            var user = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
            if (user != null)
                currentRoom.SendPacket(new UpdateFavouriteGroupComposer(null, user.VirtualId));
            currentRoom.SendPacket(new RefreshFavouriteGroupComposer(habbo.Id));
        }
        else
            session.Send(new RefreshFavouriteGroupComposer(habbo.Id));
        return Task.CompletedTask;
    }
}
