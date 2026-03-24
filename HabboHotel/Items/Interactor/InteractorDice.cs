using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Interactor;

public class InteractorDice : IFurniInteractor
{
    public void OnPlace(GameClient session, Item item)
    {
        if (item.LegacyDataString == "-1")
        {
            item.LegacyDataString = "0";
            item.UpdateNeeded = true;
        }
    }

    public void OnRemove(GameClient session, Item item)
    {
        if (item.LegacyDataString == "-1") item.LegacyDataString = "0";
    }

    public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
    {
        RoomUser user = null;
        var habbo = session?.GetHabbo();
        if (habbo != null)
            user = item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return;
        if (Gamemap.TilesTouching(item.GetX, item.GetY, user.X, user.Y))
        {
            if (item.LegacyDataString != "-1")
            {
                if (request == -1)
                {
                    item.LegacyDataString = "0";
                    item.UpdateState();
                }
                else
                {
                    item.LegacyDataString = "-1";
                    item.UpdateState(false, true);
                    item.RequestUpdate(3, true);
                }
            }
        }
        else
            user.MoveTo(item.SquareInFront);
    }

    public void OnWiredTrigger(Item item)
    {
        item.LegacyDataString = "-1";
        item.UpdateState(false, true);
        item.RequestUpdate(4, true);
    }
}
