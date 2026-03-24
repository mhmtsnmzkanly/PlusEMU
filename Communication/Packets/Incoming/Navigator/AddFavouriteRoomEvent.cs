using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Navigator;

public class AddFavouriteRoomEvent : IPacketEvent
{
    private readonly IDatabase _database;

    public AddFavouriteRoomEvent(IDatabase database)
    {
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var roomId = packet.ReadUInt();
        if (!RoomFactory.TryGetData(roomId, out var data))
            return Task.CompletedTask;
        if (data == null || habbo.FavoriteRooms.Count >= 30 || habbo.FavoriteRooms.Contains(roomId))
        {
            // send packet that favourites is full.
            return Task.CompletedTask;
        }
        habbo.FavoriteRooms.Add(roomId);
        session.Send(new UpdateFavouriteRoomComposer(roomId, true));
        using var dbClient = _database.GetQueryReactor();
        dbClient.RunQuery($"INSERT INTO user_favorites (user_id,room_id) VALUES ({habbo.Id},{roomId})");
        return Task.CompletedTask;
    }
}
