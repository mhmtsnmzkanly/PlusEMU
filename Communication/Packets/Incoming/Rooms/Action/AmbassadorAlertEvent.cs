using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Ambassadors;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class AmbassadorAlertEvent : IPacketEvent
{
    private readonly IAmbassadorsManager _ambassadorsManager;

    public AmbassadorAlertEvent(IAmbassadorsManager ambassadorsManager) => _ambassadorsManager = ambassadorsManager;

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var userid = packet.ReadInt();
        var target = PlusEnvironment.GetHabboById(userid);
        if (target == null)
            return;

        await _ambassadorsManager.Warn(session.GetHabbo(), target, "Alert");
    }
}
