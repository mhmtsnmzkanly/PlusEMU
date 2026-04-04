using Plus.HabboHotel.GameClients;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Guides;

[Singleton]
public interface IGuardianService
{
    Task SetOnDuty(GameClient session, bool onDuty);
    Task<bool> SubmitReport(GameClient reporterSession, GameClient reportedSession);
    Task AcceptTicket(GameClient session, bool accepted);
    Task Vote(GameClient session, int voteType);
    Task IgnoreUpdates(GameClient session);
    int GuardiansOnDuty { get; }
}
