namespace Plus.HabboHotel.Items;

public interface IItemHopperFinder
{
    uint GetAHopper(uint curRoom);
    uint GetHopperId(uint nextRoom);
}
