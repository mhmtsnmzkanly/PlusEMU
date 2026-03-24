using Plus.Communication.Packets.Outgoing.Users;
using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Users;

internal class GetRelationshipsEvent : IPacketEvent
{
    private readonly IMessengerService _messengerService;

    public GetRelationshipsEvent(IMessengerService messengerService)
    {
        _messengerService = messengerService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _messengerService.GetRelationships(session, packet.ReadInt());
}
