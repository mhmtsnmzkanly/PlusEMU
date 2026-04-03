using Microsoft.Extensions.DependencyInjection;
using Plus.HabboHotel.Groups;

namespace Plus.HabboHotel.Rooms;

internal class RoomDependencyResolver : IRoomDependencyResolver
{
    private readonly IServiceProvider _serviceProvider;

    public RoomDependencyResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IRoomManager GetRoomManager() => _serviceProvider.GetRequiredService<IRoomManager>();

    public IRoomService GetRoomService() => _serviceProvider.GetRequiredService<IRoomService>();

    public IGroupManager GetGroupManager() => _serviceProvider.GetRequiredService<IGroupManager>();
}
