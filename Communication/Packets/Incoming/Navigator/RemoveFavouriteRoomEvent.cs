using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Database;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Navigator;

public class RemoveFavouriteRoomEvent : IPacketEvent
{
    private readonly IDatabase _database;

    public RemoveFavouriteRoomEvent(IDatabase database)
    {
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var id = packet.ReadUInt();
        habbo.FavoriteRooms.Remove(id);
        session.Send(new UpdateFavouriteRoomComposer(id, false));
        using var dbClient = _database.GetQueryReactor();
        dbClient.RunQuery($"DELETE FROM user_favorites WHERE user_id = {habbo.Id} AND room_id = {id} LIMIT 1");
        return Task.CompletedTask;
    }
}
