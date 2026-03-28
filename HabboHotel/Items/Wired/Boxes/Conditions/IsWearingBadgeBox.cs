using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Conditions;

internal class IsWearingBadgeBox : IWiredItem, IWiredActorExecutable
{
    public IsWearingBadgeBox(Room instance, Item item)
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
        _ = packet.ReadInt();
        var badgeCode = packet.ReadString();
        StringData = badgeCode;
    }

    bool IWiredActorExecutable.Execute(WiredActorExecutionContext context)
    {
        if (string.IsNullOrEmpty(StringData))
            return false;
        var player = context.Actor;
        if (player == null)
            return false;
        var badges = player.Inventory?.Badges;
        return badges != null && badges.EquippedBadges.Any(badge => string.Equals(badge.Code, StringData, StringComparison.Ordinal));
    }
}
