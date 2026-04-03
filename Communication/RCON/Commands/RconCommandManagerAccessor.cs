using Microsoft.Extensions.DependencyInjection;

namespace Plus.Communication.RCON.Commands;

internal class RconCommandManagerAccessor : IRconCommandManagerAccessor
{
    private readonly IServiceProvider _serviceProvider;

    public RconCommandManagerAccessor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ICommandManager Get() => _serviceProvider.GetRequiredService<ICommandManager>();
}
