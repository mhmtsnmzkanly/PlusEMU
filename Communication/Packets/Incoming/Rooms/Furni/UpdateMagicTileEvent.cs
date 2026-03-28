using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Furni;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni;

internal class UpdateMagicTileEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { InRoom: true } habbo || !habbo.TryGetCurrentRoom(out var room))
            return Task.CompletedTask;
        var permissions = habbo.Permissions;
        if (!room.CheckRights(session, false, true) && !(permissions?.HasRight("room_item_use_any_stack_tile") ?? false))
            return Task.CompletedTask;

        var itemId = packet.ReadUInt();
        var decimalHeight = packet.ReadInt();
        var item = room.GetRoomItemHandler().GetItem(itemId);
        if (item == null)
            return Task.CompletedTask;
        item.GetZ = decimalHeight / 100.0;
        room.SendPacket(new ObjectUpdateComposer(item));
        room.SendPacket(new UpdateMagicTileComposer(itemId, decimalHeight));
        return Task.CompletedTask;
    }
}
