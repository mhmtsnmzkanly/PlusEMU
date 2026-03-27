using Dapper;
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
        if (habbo == null) return Task.CompletedTask;
        if (habbo.RatedRooms.Contains(room.RoomId) || room.CheckRights(session, true))
            return Task.CompletedTask;
        var rating = packet.ReadInt();
        switch (rating)
        {
            case -1: room.Score--; break;
            case 1: room.Score++; break;
            default: return Task.CompletedTask;
        }
        using var db = _database.Connection();
        db.Execute("UPDATE `rooms` SET `score` = @score WHERE `id` = @id LIMIT 1", new { score = room.Score, id = room.RoomId });
        habbo.RatedRooms.Add(room.RoomId);
        session.Send(new RoomRatingComposer(room.Score, !(habbo.RatedRooms.Contains(room.RoomId) || room.CheckRights(session, true))));
        return Task.CompletedTask;
    }
}
