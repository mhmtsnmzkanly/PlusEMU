using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;
using System.Collections.Concurrent;

namespace Plus.HabboHotel.Items.Wired.Boxes.Triggers;

internal class UserSaysCommandBox : IWiredItem
{
    public UserSaysCommandBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        StringData = "";
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.TriggerUserSaysCommand;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
        var ownerOnly = packet.ReadInt();
        var message = packet.ReadString();
        BoolData = ownerOnly == 1;
        StringData = message;
    }

    public bool Execute(params object[] @params)
    {
        var player = (Habbo)@params[0];
        if (player == null || player.CurrentRoom == null || !player.InRoom)
            return false;
        var user = player.CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(player.Username);
        if (user == null)
            return false;
        if (BoolData && Instance.OwnerId != player.Id || string.IsNullOrWhiteSpace(StringData))
            return false;
        if (!PlusEnvironment.Game.ChatManager.GetCommands().TryGetCommand(StringData.Replace(":", "").ToLower(), out var chatCommand))
            return false;
        if (player.ChatCommand == chatCommand)
        {
            player.WiredInteraction = true;
            var effects = Instance.GetWired().GetEffects(this);
            var conditions = Instance.GetWired().GetConditions(this);
            foreach (var condition in conditions.ToList())
            {
                if (!condition.Execute(player))
                    return false;
                Instance.GetWired().OnEvent(condition.Item);
            }
            player.Client.Send(new WhisperComposer(user.VirtualId, StringData, 0, 0));
            //Check the ICollection to find the random addon effect.
            var hasRandomEffectAddon = effects.Count(x => x.Type == WiredBoxType.AddonRandomEffect) > 0;
            if (hasRandomEffectAddon)
            {
                //Okay, so we have a random addon effect, now lets get the IWiredItem and attempt to execute it.
                var randomBox = effects.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandomEffect);
                if (!randomBox.Execute())
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
                    if (!effect.Execute(player))
                        return false;
                    Instance.GetWired().OnEvent(effect.Item);
                }
            }
            return true;
        }
        return false;
    }
}