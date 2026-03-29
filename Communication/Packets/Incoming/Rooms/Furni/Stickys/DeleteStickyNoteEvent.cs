using Dapper;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni.Stickys;

internal class DeleteStickyNoteEvent : RoomPacketEvent
{
    private readonly IDatabase _database;

    public DeleteStickyNoteEvent(IDatabase database)
    {
        _database = database;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        if (!room.CheckRights(session)) return Task.CompletedTask;
        var item = room.GetRoomItemHandler().GetItem(packet.ReadUInt());
        if (item == null) return Task.CompletedTask;
        if (item.Definition.IsStickyNoteOrPhoto)
        {
            room.GetRoomItemHandler().RemoveFurniture(session, item.Id);
            using var db = _database.Connection();
            db.Execute("DELETE FROM `items` WHERE `id` = @id LIMIT 1", new { id = item.Id });
        }
        return Task.CompletedTask;
    }
}
