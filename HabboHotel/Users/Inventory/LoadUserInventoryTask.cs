using Plus.HabboHotel.Badges;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users.Inventory.Bots;
using Plus.HabboHotel.Users.Inventory.Pets;
using Plus.HabboHotel.Users.UserData;

namespace Plus.HabboHotel.Users.Inventory;

internal class LoadUserInventoryTask : IUserDataLoadingTask
{
    private readonly IBadgeManager _badgeManager;
    private readonly IPetLoader _petLoader;
    private readonly IBotLoader _botLoader;
    private readonly IItemLoader _itemLoader;

    public LoadUserInventoryTask(IBadgeManager badgeManager, IPetLoader petLoader, IBotLoader botLoader, IItemLoader itemLoader)
    {
        _badgeManager = badgeManager;
        _petLoader = petLoader;
        _botLoader = botLoader;
        _itemLoader = itemLoader;
    }

    public async Task Load(Habbo habbo)
    {
        var items = _itemLoader.GetItemsForUser((uint)habbo.Id);
        habbo.Inventory = new()
        {
            Badges = new((await _badgeManager.LoadBadgesForHabbo(habbo.Id)).ToDictionary(badge => badge.Code)),
            Furniture = new(items.Where(i => i.IsFloorItem).ToList(), items.Where(i => i.IsWallItem).ToList()),
            Pets = new(_petLoader.GetPetsForUser(habbo.Id)),
            Bots = new(_botLoader.GetBotsForUser(habbo.Id))
        };
    }
}