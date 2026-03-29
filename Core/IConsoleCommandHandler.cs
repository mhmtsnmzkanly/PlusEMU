using Plus.Utilities.DependencyInjection;

namespace Plus.Core;

[Singleton]
public interface IConsoleCommandHandler
{
    void InvokeCommand(string inputData);
}
