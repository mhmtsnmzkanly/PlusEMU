namespace Plus.Core;

internal sealed class ServerRuntimeState : IServerRuntimeState
{
    public DateTime StartedAt { get; private set; }

    public void MarkStarted(DateTime startedAt)
    {
        StartedAt = startedAt;
    }
}
