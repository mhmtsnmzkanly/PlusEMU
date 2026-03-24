using Dapper;
using Plus.Database;
using Plus.HabboHotel.Users.UserData;

namespace Plus.HabboHotel.Users.Clothing;

public interface IWardrobeLoader
{
    Task<Wardrobe?> LoadUserWardrobe(int userId);
}


public class WardrobeLoader : IWardrobeLoader
{
    private readonly IUserDataFactory _userDataFactory;
    private readonly IDatabase _database;

    public WardrobeLoader(IUserDataFactory userDataFactory, IDatabase database)
    {
        _userDataFactory = userDataFactory;
        _database = database;
    }

    public async Task<Wardrobe?> LoadUserWardrobe(int userId)
    {
        if (!await _userDataFactory.HabboExists(userId))
            return null;

        using var connection = _database.Connection();
        var savedLooks = await connection.QueryAsync<SavedLook>("SELECT `slot_id`,`look`,`gender` FROM `user_wardrobe` WHERE `user_id` = @userId", new { userId });
        return new()
        {
            SavedLooks = savedLooks.ToList()
        };
    }
}

public class Wardrobe
{
    public List<SavedLook> SavedLooks { get; set; } = new();
}

public class SavedLook
{
    public int SlotId { get; set; }
    public string Look { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
}
