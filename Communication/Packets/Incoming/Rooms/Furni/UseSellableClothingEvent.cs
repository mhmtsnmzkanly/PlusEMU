using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Clothing;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni;

internal class UseSellableClothingEvent : IPacketEvent
{
    private readonly IAvatarClothingService _avatarClothingService;

    public UseSellableClothingEvent(IAvatarClothingService avatarClothingService)
    {
        _avatarClothingService = avatarClothingService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _avatarClothingService.UseSellableClothing(session, packet.ReadUInt());
}
