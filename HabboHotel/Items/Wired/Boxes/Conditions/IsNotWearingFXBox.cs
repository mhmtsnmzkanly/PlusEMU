using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Conditions;

internal class IsNotWearingFxBox : IWiredItem, IWiredActorExecutable
{
    public IsNotWearingFxBox(Room instance, Item item)
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

    bool IWiredActorExecutable.Execute(WiredActorExecutionContext context)
    {
        if (!WiredConditionDataParser.TryParseSingleValue(StringData, out var effectId))
            return false;
        var player = context.Actor;
        var effects = player?.Effects;
        if (effects == null)
            return false;
        if (effects.CurrentEffect != effectId)
            return true;
        return false;
    }
}
