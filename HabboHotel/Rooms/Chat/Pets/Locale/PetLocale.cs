using Dapper;

namespace Plus.HabboHotel.Rooms.Chat.Pets.Locale;

public class PetLocale : IPetLocale
{
    private sealed class PetLocaleRow
    {
        public string? Key { get; init; }

        public string? Value { get; init; }
    }

    private Dictionary<string, string[]> _values;

    public PetLocale()
    {
        _values = new();
    }

    public void Init()
    {
        _values = new();
        using var connection = PlusEnvironment.DatabaseManager.Connection();
        var pets = connection.Query<PetLocaleRow>("SELECT `key` AS Key, `value` AS Value FROM `bots_pet_responses`");
        foreach (var row in pets)
        {
            if (string.IsNullOrEmpty(row.Key) || string.IsNullOrEmpty(row.Value))
                continue;
            _values.Add(row.Key, row.Value.Split(';'));
        }
    }

    public string[] GetValue(string key)
    {
        if (_values.TryGetValue(key, out var value))
            return value;
        return new[] { $"Unknown pet speach:{key}" };
    }
}
