using Dapper;
using Plus.Database;
using Plus.HabboHotel.Rooms.AI;

namespace Plus.HabboHotel.Users.Inventory.Pets;

internal class PetLoader : IPetLoader
{
    private readonly IDatabase _database;

    public PetLoader(IDatabase database)
    {
        _database = database;
    }

    public List<Pet> GetPetsForUser(int userId)
    {
        using var db = _database.Connection();
        var rows = db.Query(
            @"SELECT b.`id`, b.`user_id`, b.`room_id`, b.`name`, b.`x`, b.`y`, b.`z`,
                     p.`type`, p.`race`, p.`color`, p.`experience`, p.`energy`, p.`nutrition`, p.`respect`,
                     p.`createstamp`, p.`have_saddle`, p.`anyone_ride`, p.`hairdye`, p.`pethair`, p.`gnome_clothing`
              FROM `bots` b
              INNER JOIN `bots_petdata` p ON p.`id` = b.`id`
              WHERE b.`user_id` = @userId AND b.`room_id` = '0' AND b.`ai_type` = 'pet'",
            new { userId });
        var pets = new List<Pet>();
        foreach (var row in rows)
        {
            pets.Add(new(
                (int)row.id,
                (int)row.user_id,
                Convert.ToUInt32(row.room_id),
                ((string?)row.name) ?? string.Empty,
                (int)row.type,
                ((string?)row.race) ?? string.Empty,
                ((string?)row.color) ?? string.Empty,
                (int)row.experience,
                (int)row.energy,
                (int)row.nutrition,
                (int)row.respect,
                Convert.ToDouble(row.createstamp),
                (int)row.x,
                (int)row.y,
                Convert.ToDouble(row.z),
                (int)row.have_saddle,
                (int)row.anyone_ride,
                (int)row.hairdye,
                (int)row.pethair,
                ((string?)row.gnome_clothing) ?? string.Empty));
        }
        return pets;
    }
}
