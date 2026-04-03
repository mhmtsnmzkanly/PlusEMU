using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Games.Teams;

namespace Plus.HabboHotel.Items.Interactor;

internal class InteractorFreezeTile : IFurniInteractor
{
    public void OnPlace(GameClient session, Item item) { }

    public void OnRemove(GameClient session, Item item) { }

    public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
    {
        var habbo = session?.GetHabbo();
        if (habbo == null || item == null || item.InteractingUser > 0 || !habbo.TryGetCurrentRoom(out _))
            return;
        if (!item.GetRoom().GetRoomUserManager().TryGetRoomUserByHabbo(habbo.Id, out var user) || user == null)
            return;
        if (user.Team != Team.None)
        {
            user.FreezeInteracting = true;
            item.InteractingUser = habbo.Id;
            if (item.Definition.IsFreezeTileBlock)
            {
                if (Gamemap.TileDistance(user.X, user.Y, item.GetX, item.GetY) < 2)
                    item.GetRoom().GetFreeze().OnFreezeTiles(item, item.FreezePowerUp);
            }
        }
    }

    public void OnWiredTrigger(Item item) { }
}
