using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired.Boxes.Triggers;

internal class RepeaterBox : IWiredItem, IWiredCycle
{
    private int _delay;

    public RepeaterBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public int Delay
    {
        get => _delay;
        set
        {
            _delay = value;
            TickCount = value;
        }
    }

    public int TickCount { get; set; }

    public bool OnCycle()
    {
        var success = false;
        ICollection<RoomUser> avatars = Instance.GetRoomUserManager().GetRoomUsers().ToList();
        var effects = Instance.GetWired().GetEffects(this);
        var conditions = Instance.GetWired().GetConditions(this);
        foreach (var condition in conditions.ToList())
        {
            foreach (var avatar in avatars.ToList())
            {
                var client = avatar?.GetClient();
                var habbo = client?.GetHabbo();
                if (habbo == null)
                    continue;
                if (!condition.Execute(habbo))
                    continue;
                success = true;
            }
            if (!success)
                return false;
            success = false;
            Instance.GetWired().OnEvent(condition.Item);
        }
        success = false;

        //Check the ICollection to find the random addon effect.
        var hasRandomEffectAddon = effects.Count(x => x.Type == WiredBoxType.AddonRandomEffect) > 0;
        if (hasRandomEffectAddon)
        {
            //Okay, so we have a random addon effect, now lets get the IWiredItem and attempt to execute it.
            var randomBox = effects.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandomEffect);
            if (randomBox == null || !randomBox.Execute())
                return false;

            //Success! Let's get our selected box and continue.
            var selectedBox = Instance.GetWired().GetRandomEffect(effects.ToList());
            if (!selectedBox.Execute())
                return false;

            //Woo! Almost there captain, now lets broadcast the update to the room instance.
            if (Instance != null)
            {
                Instance.GetWired().OnEvent(randomBox.Item);
                Instance.GetWired().OnEvent(selectedBox.Item);
            }
        }
        else
        {
            foreach (var effect in effects.ToList())
            {
                if (!effect.Execute())
                    continue;
                success = true;
                if (!success)
                    return false;
                if (Instance != null)
                    Instance.GetWired().OnEvent(effect.Item);
            }
        }
        TickCount = Delay;
        return true;
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.TriggerRepeat;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
        var delay = packet.ReadInt();
        Delay = delay;
        TickCount = delay;
    }

    public bool Execute(params object[] @params) => true;
}
