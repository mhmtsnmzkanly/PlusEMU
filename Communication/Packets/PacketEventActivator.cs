using Microsoft.Extensions.DependencyInjection;

namespace Plus.Communication.Packets;

internal class PacketEventActivator : IPacketEventActivator
{
    private readonly IServiceProvider _serviceProvider;

    public PacketEventActivator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool TryActivate(Type packetType, out IPacketEvent packetEvent)
    {
        if (_serviceProvider.GetService(packetType) is IPacketEvent resolvedPacketEvent)
        {
            packetEvent = resolvedPacketEvent;
            return true;
        }

        packetEvent = null!;
        return false;
    }
}
