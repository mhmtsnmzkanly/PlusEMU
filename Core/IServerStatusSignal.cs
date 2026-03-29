using Plus.Utilities.DependencyInjection;

namespace Plus.Core;

[Singleton]
public interface IServerStatusSignal
{
    void MarkDirty();
    bool ConsumeDirty();
}
