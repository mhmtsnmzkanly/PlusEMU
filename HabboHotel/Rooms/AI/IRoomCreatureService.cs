using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Rooms.AI;

public interface IRoomCreatureService
{
    Task PlacePet(Room room, GameClient session, int petId, int x, int y);
    Task PickUpPet(Room room, GameClient session, int petId);
    Task RespectPet(Room room, GameClient session, int petId);
    Task GetPetInformation(GameClient session, int petId);
    Task GetPetTrainingPanel(GameClient session, int petId);
    Task RideHorse(Room room, GameClient session, int petId, bool mount);
    Task ApplyHorseEffect(Room room, GameClient session, uint itemId, int petId);
    Task RemoveSaddleFromHorse(GameClient session, int petId);
    Task PlaceBot(Room room, GameClient session, int botId, int x, int y);
    Task PickUpBot(GameClient session, int botId);
    Task OpenBotAction(GameClient session, int botId, int actionId);
    Task SaveBotAction(GameClient session, int botId, int actionId, string dataString);
}
