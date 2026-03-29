using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni;

internal class SaveBrandingItemEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var room))
            return Task.CompletedTask;
        var permissions = habbo.Permissions;
        if (!room.CheckRights(session, true) || !(permissions?.HasRight("room_item_save_branding_items") ?? false))
            return Task.CompletedTask;

        var itemId = packet.ReadUInt();
        var item = room.GetRoomItemHandler().GetItem(itemId);
        if (item == null)
            return Task.CompletedTask;
        if (item.Definition.InteractionType == InteractionType.Background)
        {
            var data = packet.ReadInt();
            var brandData = $"state{Convert.ToChar(9)}0";
            for (var i = 1; i <= data; i++)
                brandData = brandData + Convert.ToChar(9) + packet.ReadString();
            item.LegacyDataString = brandData;
        }
        else if (item.Definition.InteractionType == InteractionType.FxProvider)
        {
            /*int Unknown = Packet.PopInt();
            string Data = Packet.PopString();
            int EffectId = Packet.PopInt();

            Item.ExtraData = Convert.ToString(EffectId);*/
        }
        room.GetRoomItemHandler().SetFloorItem(session, item, item.GetX, item.GetY, item.Rotation, false, false, true);
        return Task.CompletedTask;
    }
}
