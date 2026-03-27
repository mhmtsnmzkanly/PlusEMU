using Dapper;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Items.Interactor;

public class InteractorHopper : IFurniInteractor
{
    public void OnPlace(GameClient session, Item item)
    {
        item.GetRoom().GetRoomItemHandler().HopperCount++;
        using var db = PlusEnvironment.DatabaseManager.Connection();
        db.Execute(
            "INSERT INTO `items_hopper` (`hopper_id`, `room_id`) VALUES (@hopperid, @roomid)",
            new { hopperid = item.Id, roomid = item.RoomId });
        if (item.InteractingUser != 0)
        {
            var user = item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(item.InteractingUser);
            if (user != null)
            {
                user.ClearMovement(true);
                user.AllowOverride = false;
                user.CanWalk = true;
            }
            item.InteractingUser = 0;
        }
    }

    public void OnRemove(GameClient session, Item item)
    {
        item.GetRoom().GetRoomItemHandler().HopperCount--;
        using var db = PlusEnvironment.DatabaseManager.Connection();
        db.Execute(
            "DELETE FROM `items_hopper` WHERE `item_id` = @hid OR `room_id` = @roomId LIMIT 1",
            new { hid = item.Id, roomId = item.GetRoom().RoomId });
        if (item.InteractingUser != 0)
        {
            var user = item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(item.InteractingUser);
            if (user != null) user.UnlockWalking();
            item.InteractingUser = 0;
        }
    }

    public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
    {
        var habbo = session?.GetHabbo();
        if (item == null || item.GetRoom() == null || habbo == null)
            return;
        var user = item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null) return;

        // Alright. But is this user in the right position?
        if (user.Coordinate == item.Coordinate || user.Coordinate == item.SquareInFront)
        {
            // Fine. But is this tele even free?
            if (item.InteractingUser != 0) return;
            user.TeleDelay = 2;
            item.InteractingUser = habbo.Id;
        }
        else if (user.CanWalk) user.MoveTo(item.SquareInFront);
    }

    public void OnWiredTrigger(Item item) { }
}
