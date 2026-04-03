using Microsoft.Extensions.DependencyInjection;
using Plus.Core;
using Plus.HabboHotel.Groups;

namespace Plus.HabboHotel.Rooms;

internal class RoomDependencyResolver : IRoomDependencyResolver
{
    private readonly IServiceProvider _serviceProvider;

    public RoomDependencyResolver(IServiceProvider serviceProvider)
    {
        BootProbe.Write("Entering RoomDependencyResolver constructor...");
        _serviceProvider = serviceProvider;
        BootProbe.Write("Leaving RoomDependencyResolver constructor.");
    }

    public IRoomManager GetRoomManager() => _serviceProvider.GetRequiredService<IRoomManager>();

    public IRoomService GetRoomService() => _serviceProvider.GetRequiredService<IRoomService>();

    public IGroupManager GetGroupManager() => _serviceProvider.GetRequiredService<IGroupManager>();
}
