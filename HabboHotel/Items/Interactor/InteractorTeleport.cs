using Plus.HabboHotel.GameClients;
using Plus.Utilities;

namespace Plus.HabboHotel.Items.Interactor;

public class InteractorTeleport : IFurniInteractor
{
    public void OnPlace(GameClient session, Item item)
    {
        item.LegacyDataString = "0";
        if (item.InteractingUser != 0)
        {
            if (item.GetRoom().GetRoomUserManager().TryGetRoomUserByHabbo(item.InteractingUser, out var user) && user != null)
            {
                user.ClearMovement(true);
                user.AllowOverride = false;
                user.CanWalk = true;
            }
            item.InteractingUser = 0;
        }
        if (item.InteractingUser2 != 0)
        {
            if (item.GetRoom().GetRoomUserManager().TryGetRoomUserByHabbo(item.InteractingUser2, out var user) && user != null)
            {
                user.ClearMovement(true);
                user.AllowOverride = false;
                user.CanWalk = true;
            }
            item.InteractingUser2 = 0;
        }
    }

    public void OnRemove(GameClient session, Item item)
    {
        item.LegacyDataString = "0";
        if (item.InteractingUser != 0)
        {
            if (item.GetRoom().GetRoomUserManager().TryGetRoomUserByHabbo(item.InteractingUser, out var user) && user != null)
                user.UnlockWalking();
            item.InteractingUser = 0;
        }
        if (item.InteractingUser2 != 0)
        {
            if (item.GetRoom().GetRoomUserManager().TryGetRoomUserByHabbo(item.InteractingUser2, out var user) && user != null)
                user.UnlockWalking();
            item.InteractingUser2 = 0;
        }
    }

    public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
    {
        var habbo = session?.GetHabbo();
        if (item == null || item.GetRoom() == null || habbo == null)
            return;
        if (!item.GetRoom().GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var user) || user == null)
            return;
        user.LastInteraction = UnixTimestamp.GetNow();

        // Alright. But is this user in the right position?
        if (user.Coordinate == item.Coordinate || user.Coordinate == item.SquareInFront)
        {
            // Fine. But is this tele even free?
            if (item.InteractingUser != 0) return;
            if (!user.CanWalk || habbo.IsTeleporting || habbo.TeleporterId != 0 ||
                user.LastInteraction + 2 - UnixTimestamp.GetNow() < 0)
                return;
            user.TeleDelay = 2;
            item.InteractingUser = habbo.Id;
        }
        else if (user.CanWalk) user.MoveTo(item.SquareInFront);
    }

    public void OnWiredTrigger(Item item) { }
}
