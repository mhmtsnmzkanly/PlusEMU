using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Clothing;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni;

internal class SetMannequinFigureEvent : IPacketEvent
{
    private readonly IAvatarClothingService _avatarClothingService;

    public SetMannequinFigureEvent(IAvatarClothingService avatarClothingService)
    {
        _avatarClothingService = avatarClothingService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _avatarClothingService.SetMannequinFigure(session, packet.ReadUInt());
}
