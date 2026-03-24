using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModeratorActionEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo?.Permissions == null || !habbo.Permissions.HasRight("mod_caution"))
            return Task.CompletedTask;
        if (!habbo.InRoom)
            return Task.CompletedTask;
        var currentRoom = habbo.CurrentRoom;
        if (currentRoom == null)
            return Task.CompletedTask;
        var alertMode = packet.ReadInt();
        var alertMessage = packet.ReadString();
        var isCaution = alertMode != 3;
        alertMessage = isCaution ? $"Caution from Moderator:\n\n{alertMessage}" : $"Message from Moderator:\n\n{alertMessage}";
        currentRoom.SendPacket(new BroadcastMessageAlertComposer(alertMessage));
        return Task.CompletedTask;
    }
}
