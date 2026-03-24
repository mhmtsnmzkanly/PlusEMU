using System.Collections.Generic;
using Plus.Communication.Packets.Outgoing.Users;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Ignores;

namespace Plus.Communication.Packets.Incoming.Users;

internal class GetIgnoredUsersEvent : IPacketEvent
{
    private readonly IIgnoredUsersService _ignoredUsersService;

    public GetIgnoredUsersEvent(IIgnoredUsersService ignoredUsersService)
    {
        _ignoredUsersService = ignoredUsersService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        var ignoredUserIds = session.GetHabbo()?.IgnoresComponent?.IgnoredUsers ?? new List<int>();
        var ignoredUsers = await _ignoredUsersService.GetIgnoredUsersByName(ignoredUserIds);
        session.Send(new IgnoredUsersComposer(ignoredUsers));
    }
}
