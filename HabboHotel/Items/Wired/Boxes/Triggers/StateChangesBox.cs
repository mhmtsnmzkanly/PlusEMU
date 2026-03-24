using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Triggers;

internal class StateChangesBox : IWiredItem
{
    public StateChangesBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.TriggerStateChanges;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
        var unknown2 = packet.ReadString();
        if (SetItems.Count > 0)
            SetItems.Clear();
        var furniCount = packet.ReadInt();
        for (var i = 0; i < furniCount; i++)
        {
            var selectedItem = Instance.GetRoomItemHandler().GetItem(packet.ReadUInt());
            if (selectedItem != null)
                SetItems.TryAdd(selectedItem.Id, selectedItem);
        }
    }

    public bool Execute(params object[] @params)
    {
        var player = (Habbo)@params[0];
        if (player == null)
            return false;
        var item = (Item)@params[1];
        if (item == null)
            return false;
        if (!SetItems.ContainsKey(item.Id))
            return false;
        var instance = Instance;
        if (instance == null)
            return false;
        var wired = instance.GetWired();
        var effects = wired.GetEffects(this);
        var conditions = wired.GetConditions(this);
        foreach (var condition in conditions)
        {
            if (!condition.Execute(player))
                return false;
            if (Instance != null)
                Instance.GetWired().OnEvent(condition.Item);
        }

        //Check the ICollection to find the random addon effect.
        var hasRandomEffectAddon = effects.Count(x => x.Type == WiredBoxType.AddonRandomEffect) > 0;
        if (hasRandomEffectAddon)
        {
            //Okay, so we have a random addon effect, now lets get the IWiredItem and attempt to execute it.
            var randomBox = effects.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandomEffect);
            if (randomBox == null || !randomBox.Execute())
                return false;

            //Success! Let's get our selected box and continue.
            var selectedBox = wired.GetRandomEffect(effects.ToList());
            if (selectedBox == null || !selectedBox.Execute())
                return false;

            //Woo! Almost there captain, now lets broadcast the update to the room instance.
            wired.OnEvent(randomBox.Item);
            wired.OnEvent(selectedBox.Item);
        }
        else
        {
            foreach (var effect in effects)
            {
                if (effect == null || !effect.Execute(player))
                    return false;
                wired.OnEvent(effect.Item);
            }
        }
        return true;
    }
}
