using System.Data;

namespace Plus.HabboHotel.Rooms.Chat.Pets.Locale;

public class PetLocale : IPetLocale
{
    private Dictionary<string, string[]> _values;

    public PetLocale()
    {
        _values = new();
    }

    public void Init()
    {
        _values = new();
        using var dbClient = PlusEnvironment.DatabaseManager.GetQueryReactor();
        dbClient.SetQuery("SELECT * FROM `bots_pet_responses`");
        var pets = dbClient.GetTable();
        if (pets != null)
            foreach (DataRow row in pets.Rows)
            {
                var key = row[0].ToString();
                var value = row[1].ToString();
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                    continue;
                _values.Add(key, value.Split(';'));
            }
    }

    public string[] GetValue(string key)
    {
        if (_values.TryGetValue(key, out var value))
            return value;
        return new[] { $"Unknown pet speach:{key}" };
    }
}
