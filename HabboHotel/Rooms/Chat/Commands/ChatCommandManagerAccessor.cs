using Microsoft.Extensions.DependencyInjection;

namespace Plus.HabboHotel.Rooms.Chat.Commands;

internal class ChatCommandManagerAccessor : IChatCommandManagerAccessor
{
    private readonly IServiceProvider _serviceProvider;

    public ChatCommandManagerAccessor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ICommandManager Get() => _serviceProvider.GetRequiredService<ICommandManager>();
}
