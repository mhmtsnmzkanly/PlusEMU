using Plus.Communication.Packets.Outgoing.Groups;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni;

internal class GetGroupFurniSettingsEvent : IPacketEvent
{
    private readonly IGroupManager _groupManager;
    private readonly IRoomFactory _roomFactory;

    public GetGroupFurniSettingsEvent(IGroupManager groupManager, IRoomFactory roomFactory)
    {
        _groupManager = groupManager;
        _roomFactory = roomFactory;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.TryGetCurrentRoom(out var currentRoom))
            return Task.CompletedTask;

        var itemId = packet.ReadUInt();
        var groupId = packet.ReadInt();
        var item = currentRoom.GetRoomItemHandler().GetItem(itemId);
        if (item == null)
            return Task.CompletedTask;

        if (item.Definition.InteractionType != InteractionType.GuildGate)
            return Task.CompletedTask;
        if (!_groupManager.TryGetGroup(groupId, out var group))
            return Task.CompletedTask;

        session.Send(new GroupFurniSettingsComposer(group, itemId, habbo.Id));
        session.Send(new GroupInfoComposer(group, session, _roomFactory));
        return Task.CompletedTask;
    }
}
