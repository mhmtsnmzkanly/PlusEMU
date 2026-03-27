using Dapper;
using Plus.Database;

namespace Plus.HabboHotel.Rooms.Chat.Pets.Locale;

public class PetLocale : IPetLocale
{
    private readonly IDatabase _database;

    private sealed class PetLocaleRow
    {
        public string? Key { get; init; }

        public string? Value { get; init; }
    }

    private Dictionary<string, string[]> _values;

    public PetLocale(IDatabase database)
    {
        _database = database;
        _values = new();
    }

    public void Init()
    {
        _values = new();
        using var connection = _database.Connection();
        var pets = connection.Query<PetLocaleRow>("SELECT `pet_id` AS `Key`, `responses` AS `Value` FROM `bots_pet_responses`");
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
