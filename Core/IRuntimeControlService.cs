using Plus.Utilities.DependencyInjection;

namespace Plus.Core;

[Singleton]
public interface IRuntimeControlService
{
    void BroadcastAlert(string message);
    void PerformShutdown(string? reason = null);
}
