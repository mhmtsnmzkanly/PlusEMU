using Dapper;
using Plus.HabboHotel.Rooms.AI;
using Plus.Utilities;

namespace Plus.HabboHotel.Catalog.Utilities;

public static class PetUtility
{
    public static bool CheckPetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        if (name.Length < 1 || name.Length > 16)
            return false;
        if (!StringCharFilter.IsValidAlphaNumeric(name))
            return false;
        return true;
    }

    public static Pet CreatePet(int userId, string name, int type, string race, string colour)
    {
        var pet = new Pet(0, userId, 0, name, type, race, colour, 0, 100, 100, 0, UnixTimestamp.GetNow(), 0, 0, 0.0, 0, 0, 0, -1, "-1");
        using var db = PlusEnvironment.DatabaseManager.Connection();
        pet.PetId = db.ExecuteScalar<int>(
            "INSERT INTO `bots` (`user_id`, `name`, `ai_type`) VALUES (@ownerId, @name, 'pet'); SELECT LAST_INSERT_ID();",
            new { ownerId = pet.OwnerId, name = pet.Name });
        db.Execute(
            "INSERT INTO `bots_petdata` (`id`, `type`, `race`, `color`, `experience`, `energy`, `createstamp`) VALUES (@id, @type, @race, @color, 0, 100, UNIX_TIMESTAMP())",
            new { id = pet.PetId, type = pet.Type, race = pet.Race, color = pet.Color });
        return pet;
    }
}