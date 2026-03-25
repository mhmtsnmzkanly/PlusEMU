using Microsoft.Extensions.Logging;
using Plus.Communication.Attributes;
using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Handshake;

[NoAuthenticationRequired]
public class UniqueIdEvent : IPacketEvent
{
    private readonly IModerationManager _moderationManager;
    private readonly ILogger<UniqueIdEvent> _logger;

    public UniqueIdEvent(IModerationManager moderationManager, ILogger<UniqueIdEvent> logger)
    {
        _moderationManager = moderationManager;
        _logger = logger;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        packet.ReadString();
        var machineId = packet.ReadString();
        _logger.LogInformation("Received UniqueIdEvent for session {sessionId}. Build: {build}.", session.Id, session.ClientBuild ?? "<unknown>");
        session.MachineId = machineId;
        if (_moderationManager.HasMachineBanCheck(machineId))
        {
            session.Disconnect($"Machine ban matched: {machineId}");
            return Task.CompletedTask;
        }
        session.Send(new SetUniqueIdComposer(machineId));
        return Task.CompletedTask;
    }
}
