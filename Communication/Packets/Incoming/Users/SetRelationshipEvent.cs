using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.Friends;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Users;

internal class SetRelationshipEvent : IPacketEvent
{
    private readonly IMessengerService _messengerService;

    public SetRelationshipEvent(IMessengerService messengerService)
    {
        _messengerService = messengerService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _messengerService.SetRelationship(session, packet.ReadInt(), packet.ReadInt());
}
