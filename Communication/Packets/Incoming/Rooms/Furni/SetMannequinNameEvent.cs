using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Clothing;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni;

internal class SetMannequinNameEvent : IPacketEvent
{
    private readonly IAvatarClothingService _avatarClothingService;

    public SetMannequinNameEvent(IAvatarClothingService avatarClothingService)
    {
        _avatarClothingService = avatarClothingService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _avatarClothingService.SetMannequinName(session, packet.ReadUInt(), packet.ReadString());
}
