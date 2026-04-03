using Plus.Database;
using System.Collections.Concurrent;
using Dapper;
using Plus.HabboHotel.Users.Clothing.Parts;

namespace Plus.HabboHotel.Users.Clothing;

public sealed class ClothingComponent
{
    private sealed class UserClothingRow
    {
        public int Id { get; init; }

        public int PartId { get; init; }

        public string? Part { get; init; }
    }

    /// <summary>
    /// Effects stored by ID > Effect.
    /// </summary>
    private readonly ConcurrentDictionary<int, ClothingParts> _allClothing = new();
    private Habbo? _habbo;
    private IDatabase? _database;

    public ICollection<ClothingParts> GetClothingParts => _allClothing.Values;

    /// <summary>
    /// Initializes the EffectsComponent.
    /// </summary>
    /// <param name="UserId"></param>
    public bool Init(Habbo habbo, IDatabase database)
    {
        if (_allClothing.Count > 0)
            return false;
        _database = database;
        using (var connection = _database.Connection())
        {
            var getClothing = connection.Query<UserClothingRow>("SELECT `id`,`part_id` AS PartId,`part` FROM `user_clothing` WHERE `user_id` = @id;",
                new { id = habbo.Id });
            foreach (var row in getClothing)
            {
                if (_allClothing.TryAdd(row.PartId, new(row.Id, row.PartId, row.Part ?? string.Empty)))
                {
                    //umm?
                }
            }
        }
        _habbo = habbo;
        return true;
    }

    public void AddClothing(string clothingName, List<int> partIds)
    {
        var habbo = _habbo;
        var database = _database;
        if (habbo == null || database == null)
            return;

        foreach (var partId in partIds.ToList())
        {
            if (!_allClothing.ContainsKey(partId))
            {
                using (var connection = database.Connection())
                {
                    var newId = Convert.ToInt32(connection.ExecuteScalar<long>(
                        "INSERT INTO `user_clothing` (`user_id`,`part_id`,`part`) VALUES (@UserId, @PartId, @Part); SELECT LAST_INSERT_ID();",
                        new { UserId = habbo.Id, PartId = partId, Part = clothingName }));
                    _allClothing.TryAdd(partId, new(newId, partId, clothingName));
                }
            }
        }
    }

    public bool TryGet(int partId, out ClothingParts? clothingPart) => _allClothing.TryGetValue(partId, out clothingPart);

    /// <summary>
    /// Disposes the ClothingComponent.
    /// </summary>
    public void Dispose()
    {
        _allClothing.Clear();
    }
}
