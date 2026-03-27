using Dapper;
using Plus.Database;

namespace Plus.HabboHotel.Catalog.Pets;

public class PetRaceManager : IPetRaceManager
{
    private readonly IDatabase _database;
    private readonly List<PetRace> _races = new();

    public PetRaceManager(IDatabase database)
    {
        _database = database;
    }

    public void Init()
    {
        if (_races.Count > 0)
            _races.Clear();
        using var db = _database.Connection();
        var rows = db.Query("SELECT `raceid`, `color1`, `color2`, `has1color`, `has2color` FROM `catalog_pet_races`");
        foreach (var row in rows)
        {
            var race = new PetRace(
                (int)row.raceid,
                (int)row.color1,
                (int)row.color2,
                ((string?)row.has1color) == "1",
                ((string?)row.has2color) == "1");
            if (!_races.Contains(race))
                _races.Add(race);
        }
    }

    public List<PetRace> GetRacesForRaceId(int raceId)
    {
        return _races.Where(race => race.RaceId == raceId).ToList();
    }
}