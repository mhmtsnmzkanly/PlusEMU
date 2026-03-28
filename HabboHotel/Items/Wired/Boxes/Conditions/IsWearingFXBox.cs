using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Conditions;

internal class IsWearingFxBox : IWiredItem, IWiredExecutable
{
    public IsWearingFxBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.ConditionIsWearingFx;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
        var unknown2 = packet.ReadInt();
        StringData = unknown2.ToString();
    }

    bool IWiredExecutable.Execute(WiredExecutionContext context)
    {
        if (string.IsNullOrEmpty(StringData))
            return false;
        var player = context.Actor;
        var effects = player?.Effects;
        if (effects == null)
            return false;
        if (effects.CurrentEffect != int.Parse(StringData))
            return false;
        return true;
    }
}
