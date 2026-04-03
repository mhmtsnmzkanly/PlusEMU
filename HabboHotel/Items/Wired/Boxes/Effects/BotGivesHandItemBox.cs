using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class BotGivesHandItemBox : IWiredItem, IWiredActorExecutable
{
    public BotGivesHandItemBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectBotGivesHanditemBox;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        _ = packet.ReadInt();
        var drinkId = packet.ReadInt();
        var botName = packet.ReadString();
        if (SetItems.Count > 0)
            SetItems.Clear();
        StringData = $"{botName};{drinkId}";
    }

    bool IWiredActorExecutable.Execute(WiredActorExecutionContext context)
    {
        if (!WiredEffectDataParser.TryParseBotHandItem(StringData, out var botName, out var drinkId))
            return false;
        var player = context.Actor;
        if (player == null)
            return false;
        if (!Instance.GetRoomUserManager().TryGetRoomUserByHabbo(player.Id, out var actor) || actor == null)
            return false;
        if (!Instance.GetRoomUserManager().TryGetBotByName(botName, out var user) || user == null)
            return false;
        if (user.BotData.TargetUser == 0)
        {
            if (!Instance.GetGameMap().CanWalk(actor.SquareBehind.X, actor.SquareBehind.Y, false))
                return false;
            user.CarryItem(drinkId);
            user.BotData.TargetUser = actor.HabboId;
            user.MoveTo(actor.SquareBehind.X, actor.SquareBehind.Y);
        }
        return true;
    }
}
