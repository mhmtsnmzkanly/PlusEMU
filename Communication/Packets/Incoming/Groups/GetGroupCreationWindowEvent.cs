using Plus.Communication.Packets.Outgoing.Groups;
using Plus.Core.Settings;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Groups;

internal class GetGroupCreationWindowEvent : IPacketEvent
{
    private readonly IRoomFactory _roomFactory;
    private readonly ISettingsManager _settingsManager;

    public GetGroupCreationWindowEvent(IRoomFactory roomFactory, ISettingsManager settingsManager)
    {
        _roomFactory = roomFactory;
        _settingsManager = settingsManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo)
            return Task.CompletedTask;

        var rooms = _roomFactory.GetRoomsDataByOwnerSortByName(habbo.Id).Where(x => x.Group == null).ToList();
        session.Send(new GroupCreationWindowComposer(rooms, _settingsManager));
        return Task.CompletedTask;
    }
}
