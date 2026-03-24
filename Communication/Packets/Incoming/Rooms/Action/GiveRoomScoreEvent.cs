using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class GiveRoomScoreEvent : RoomPacketEvent
{
    private readonly IDatabase _database;

    public GiveRoomScoreEvent(IDatabase database)
    {
        _database = database;
    }
    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;
        if (habbo.RatedRooms.Contains(room.RoomId) || room.CheckRights(session, true))
            return Task.CompletedTask;
        var rating = packet.ReadInt();
        switch (rating)
        {
            case -1:
                room.Score--;
                break;
            case 1:
                room.Score++;
                break;
            default:
                return Task.CompletedTask;
        }
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.RunQuery($"UPDATE rooms SET score = '{room.Score}' WHERE id = '{room.RoomId}' LIMIT 1");
        }
        habbo.RatedRooms.Add(room.RoomId);
        session.Send(new RoomRatingComposer(room.Score, !(habbo.RatedRooms.Contains(room.RoomId) || room.CheckRights(session, true))));
        return Task.CompletedTask;
    }
}
