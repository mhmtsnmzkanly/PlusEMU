using Plus.HabboHotel.Rooms.AI;

namespace Plus.HabboHotel.Catalog.Utilities;

public interface IPetUtility
{
    bool CheckPetName(string name);
    Pet CreatePet(int userId, string name, int type, string race, string colour);
}
