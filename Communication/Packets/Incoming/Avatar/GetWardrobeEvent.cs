using Plus.Communication.Packets.Outgoing.Avatar;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Clothing;

namespace Plus.Communication.Packets.Incoming.Avatar;

internal class GetWardrobeEvent : IPacketEvent
{
    private readonly IAvatarClothingService _avatarClothingService;

    public GetWardrobeEvent(IAvatarClothingService avatarClothingService)
    {
        _avatarClothingService = avatarClothingService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _avatarClothingService.GetWardrobe(session);
}
