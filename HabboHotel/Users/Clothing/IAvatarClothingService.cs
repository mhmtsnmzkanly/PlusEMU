using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Users.Clothing;

public interface IAvatarClothingService
{
    Task GetWardrobe(GameClient session);
    Task SaveWardrobeOutfit(GameClient session, int slotId, string look, string gender);
    Task UseSellableClothing(GameClient session, uint itemId);
    Task SetMannequinFigure(GameClient session, uint itemId);
    Task SetMannequinName(GameClient session, uint itemId, string name);
}
