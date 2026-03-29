using Plus.Utilities.DependencyInjection;

namespace Plus.Core;

[Singleton]
public interface IServerRuntimeState
{
    DateTime StartedAt { get; }
    void MarkStarted(DateTime startedAt);
}
