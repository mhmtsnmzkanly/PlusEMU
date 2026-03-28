using Plus.HabboHotel.GameClients;
using Plus.Utilities;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Avatar;

internal class ApplySignEvent : RoomPacketEvent
{
    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var signId = packet.ReadInt();
        if (session.GetHabbo() is not { } habbo)
            return Task.CompletedTask;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return Task.CompletedTask;

        user.UnIdle();
        user.SetStatus("sign", Convert.ToString(signId));
        user.UpdateNeeded = true;
        user.SignTime = UnixTimestamp.GetNow() + 5;
        return Task.CompletedTask;
    }
}
