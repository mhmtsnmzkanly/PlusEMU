using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Connection;

public class OpenFlatConnectionEvent : IPacketEvent
{
    private readonly IRoomService _roomService;

    public OpenFlatConnectionEvent(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var roomId = packet.ReadUInt();
        var password = packet.ReadString();
        return _roomService.PrepareRoom(session, roomId, password);
    }
}
