using Plus.Communication.Packets.Outgoing.Groups;
using Plus.Communication.Packets.Outgoing.Users;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Dapper;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class SetGroupFavouriteEvent : IPacketEvent
{
    private readonly IGroupManager _groupManager;
    private readonly IDatabase _database;

    public SetGroupFavouriteEvent(IGroupManager groupManager, IDatabase database)
    {
        _groupManager = groupManager;
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.HabboStats == null)
            return Task.CompletedTask;

        var groupId = packet.ReadInt();
        if (groupId == 0)
            return Task.CompletedTask;
        if (!_groupManager.TryGetGroup(groupId, out var group))
            return Task.CompletedTask;
        habbo.HabboStats.FavouriteGroupId = group.Id;
        using (var connection = _database.Connection())
        {
            connection.Execute("UPDATE `user_statistics` SET `groupid` = @groupId WHERE `id` = @userId LIMIT 1",
                new { groupId = habbo.HabboStats.FavouriteGroupId, userId = habbo.Id });
        }
        var currentRoom = habbo.CurrentRoom;
        if (habbo.InRoom && currentRoom != null)
        {
            currentRoom.SendPacket(new RefreshFavouriteGroupComposer(habbo.Id));
            currentRoom.SendPacket(new HabboGroupBadgesComposer(group));
            var user = currentRoom.GetRoomUserManager()
                .GetRoomUserByHabbo(habbo.Id);
            if (user != null)
                currentRoom.SendPacket(new UpdateFavouriteGroupComposer(group, user.VirtualId));
        }
        else
            session.Send(new RefreshFavouriteGroupComposer(habbo.Id));
        return Task.CompletedTask;
    }
}
