using Plus.Communication.Packets.Outgoing.Rooms.Furni.Wired;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.Wired;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni.Wired;

internal abstract class SaveWiredConfigEvent : IPacketEvent
{
    public virtual Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var room) || !room.CheckRights(session, false, true))
            return Task.CompletedTask;

        var permissions = habbo.Permissions;
        var itemId = packet.ReadUInt();
        session.Send(new HideWiredConfigComposer());
        var selectedItem = room.GetRoomItemHandler().GetItem(itemId);
        if (selectedItem == null)
            return Task.CompletedTask;

        if (!room.GetWired().TryGet(itemId, out var box))
            return Task.CompletedTask;
        if (box.Type == WiredBoxType.EffectGiveUserBadge && !(permissions?.HasRight("room_item_wired_rewards") ?? false))
        {
            session.SendNotification("You don't have the correct permissions to do this.");
            return Task.CompletedTask;
        }
        box.HandleSave(packet);
        room.GetWired().SaveBox(box);
        return Task.CompletedTask;
    }
}
