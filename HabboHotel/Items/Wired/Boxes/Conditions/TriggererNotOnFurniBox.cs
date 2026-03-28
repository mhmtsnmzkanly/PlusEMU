using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Conditions;

internal class TriggererNotOnFurniBox : IWiredItem, IWiredActorExecutable
{
    public TriggererNotOnFurniBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.ConditionTriggererNotOnFurni;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        _ = packet.ReadInt();
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

    bool IWiredActorExecutable.Execute(WiredActorExecutionContext context)
    {
        var player = context.Actor;
        if (player == null || !player.TryGetCurrentRoom(out var currentRoom))
            return false;
        var user = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(player.Username);
        if (user == null)
            return false;
        var itemsOnSquare = Instance.GetGameMap().GetAllRoomItemForSquare(user.X, user.Y);
        foreach (var item in itemsOnSquare.ToList())
        {
            if (SetItems.ContainsKey(item.Id))
                return false;
        }
        return true;
    }
}
