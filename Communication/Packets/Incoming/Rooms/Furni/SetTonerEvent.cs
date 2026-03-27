using Dapper;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni;

internal class SetTonerEvent : RoomPacketEvent
{
    private readonly IDatabase _database;

    public SetTonerEvent(IDatabase database)
    {
        _database = database;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        if (!room.CheckRights(session, true)) return Task.CompletedTask;
        if (room.TonerData == null) return Task.CompletedTask;
        var item = room.GetRoomItemHandler().GetItem(room.TonerData.ItemId);
        if (item == null || item.Definition.InteractionType != InteractionType.Toner) return Task.CompletedTask;
        packet.ReadInt();
        var int1 = packet.ReadInt();
        var int2 = packet.ReadInt();
        var int3 = packet.ReadInt();
        if (int1 > 255 || int2 > 255 || int3 > 255) return Task.CompletedTask;
        using var db = _database.Connection();
        db.Execute(
            "UPDATE `room_items_toner` SET `enabled` = '1', `data1` = @d1, `data2` = @d2, `data3` = @d3 WHERE `id` = @itemId LIMIT 1",
            new { d1 = int1, d2 = int2, d3 = int3, itemId = item.Id });
        room.TonerData.Hue = int1;
        room.TonerData.Saturation = int2;
        room.TonerData.Lightness = int3;
        room.TonerData.Enabled = 1;
        room.SendPacket(new ObjectUpdateComposer(item));
        item.UpdateState();
        return Task.CompletedTask;
    }
}