using System.Threading;

namespace Plus.Core;

internal sealed class ServerStatusSignal : IServerStatusSignal
{
    private int _dirty = 1;

    public void MarkDirty()
    {
        Interlocked.Exchange(ref _dirty, 1);
    }

    public bool ConsumeDirty() => Interlocked.Exchange(ref _dirty, 0) == 1;
}
