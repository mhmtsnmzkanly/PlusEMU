using Plus.Core.FigureData;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Users.Clothing;

namespace Plus.Communication.Packets.Incoming.Avatar;

internal class SaveWardrobeOutfitEvent : IPacketEvent
{
    private readonly IAvatarClothingService _avatarClothingService;

    public SaveWardrobeOutfitEvent(IAvatarClothingService avatarClothingService)
    {
        _avatarClothingService = avatarClothingService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var slotId = packet.ReadInt();
        var look = packet.ReadString();
        var gender = packet.ReadString();
        return _avatarClothingService.SaveWardrobeOutfit(session, slotId, look, gender);
    }
}
