using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Conditions;

internal class IsNotWearingBadgeBox : IWiredItem, IWiredExecutable
{
    public IsNotWearingBadgeBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.ConditionIsWearingBadge;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
        var badgeCode = packet.ReadString();
        StringData = badgeCode;
    }

    public bool Execute(params object[] @params)
    {
        return ((IWiredExecutable)this).Execute(new(@params));
    }

    bool IWiredExecutable.Execute(WiredExecutionContext context)
    {
        if (string.IsNullOrEmpty(StringData))
            return false;
        var player = context.Actor;
        if (player == null)
            return false;
        var badges = player.Inventory?.Badges;
        if (badges == null)
            return true;
        if (!badges.HasBadge(StringData))
            return true;
        var equippedBadges = badges.EquippedBadges;
        if (!equippedBadges.Any())
            return true;

        return equippedBadges.All(badge => !string.Equals(badge.Code, StringData, StringComparison.Ordinal));
    }
}
