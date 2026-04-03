using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class BotFollowsUserBox : IWiredItem, IWiredActorExecutable
{
    public BotFollowsUserBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectBotFollowsUserBox;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        _ = packet.ReadInt();
        var followMode = packet.ReadInt(); //1 = follow, 0 = don't.
        var botConfiguration = packet.ReadString();
        if (SetItems.Count > 0)
            SetItems.Clear();
        StringData = $"{followMode};{botConfiguration}";
    }

    bool IWiredActorExecutable.Execute(WiredActorExecutionContext context)
    {
        if (!WiredEffectDataParser.TryParseBotFollow(StringData, out var followMode, out var botName))
            return false;
        var player = context.Actor;
        if (player == null)
            return false;
        if (!Instance.GetRoomUserManager().TryGetRoomUserByHabbo(player.Id, out var human) || human == null)
            return false;
        if (!Instance.GetRoomUserManager().TryGetBotByName(botName, out var user) || user == null)
            return false;
        if (followMode == 0)
        {
            user.BotData.ForcedUserTargetMovement = 0;
            if (user.IsWalking)
                user.ClearMovement(true);
        }
        else if (followMode == 1)
        {
            user.BotData.ForcedUserTargetMovement = player.Id;
            if (user.IsWalking)
                user.ClearMovement(true);
            user.MoveTo(human.X, human.Y);
        }
        return true;
    }
}
