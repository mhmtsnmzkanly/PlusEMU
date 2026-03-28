using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni;

internal class ThrowDiceEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.TryGetCurrentRoom(out var room))
            return Task.CompletedTask;
        var item = room.GetRoomItemHandler().GetItem(packet.ReadUInt());
        if (item == null)
            return Task.CompletedTask;
        var hasRights = room.CheckRights(session, false, true);
        var request = packet.ReadInt();
        item.Interactor.OnTrigger(session, item, request, hasRights);
        return Task.CompletedTask;
    }
}
