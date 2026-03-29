using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms;

public class RoomItemStateService : IRoomItemStateService
{
    public RoomItemStateInitializationResult InitializeLoadedFloorItem(Room room, Item item)
    {
        if (item.IsRoller)
            return new() { HasRoller = true };

        if (item.Definition.IsMoodlight)
        {
            if (room.MoodlightData == null)
                room.MoodlightData = new(item.Id, room.GetDatabase());
            return new();
        }

        if (item.Definition.IsToner)
        {
            if (room.TonerData == null)
                room.TonerData = new(item.Id, room.GetDatabase());
            return new();
        }

        if (item.IsWired)
        {
            room.GetWired()?.LoadWiredBox(item);
            return new();
        }

        return item.Definition.IsHopper
            ? new() { HopperDelta = 1 }
            : new();
    }

    public void InitializeWallItemState(Room room, Item item)
    {
        if (!item.Definition.IsMoodlight)
            return;

        if (room.MoodlightData == null)
        {
            room.MoodlightData = new(item.Id, room.GetDatabase());
            item.LegacyDataString = room.MoodlightData.GenerateExtraData();
        }
    }

    public void EnsureTonerData(Room room, Item item)
    {
        if (!item.Definition.IsToner)
            return;

        if (room.TonerData == null)
            room.TonerData = new(item.Id, room.GetDatabase());
    }
}
