using System.Drawing;
using Plus.Communication.Packets.Outgoing.Rooms.Furni.LoveLocks;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Interactor;

public class InteractorLoveLock : IFurniInteractor
{
    public void OnPlace(GameClient session, Item item) { }

    public void OnRemove(GameClient session, Item item) { }

    public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;
        var user = item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return;
        if (Gamemap.TilesTouching(item.GetX, item.GetY, user.X, user.Y))
        {
            if (item.LegacyDataString == null || item.LegacyDataString.Length <= 1 || !item.LegacyDataString.Contains(Convert.ToChar(5).ToString()))
            {
                Point pointOne;
                Point pointTwo;
                switch (item.Rotation)
                {
                    case 2:
                        pointOne = new(item.GetX, item.GetY + 1);
                        pointTwo = new(item.GetX, item.GetY - 1);
                        break;
                    case 4:
                        pointOne = new(item.GetX - 1, item.GetY);
                        pointTwo = new(item.GetX + 1, item.GetY);
                        break;
                    default:
                        return;
                }
                var userOne = item.GetRoom().GetRoomUserManager().GetUserForSquare(pointOne.X, pointOne.Y);
                var userTwo = item.GetRoom().GetRoomUserManager().GetUserForSquare(pointTwo.X, pointTwo.Y);
                if (userOne == null || userTwo == null)
                    session.SendNotification(item.GetRoom().GetLanguageManager().Require("lovelock.user_invalid"));
                else if (userOne.GetClient() == null || userTwo.GetClient() == null)
                    session.SendNotification(item.GetRoom().GetLanguageManager().Require("lovelock.user_invalid"));
                else if (userOne.HabboId != item.UserId && userTwo.HabboId != item.UserId)
                    session.SendNotification(item.GetRoom().GetLanguageManager().Require("lovelock.owner_only"));
                else
                {
                    var userOneClient = userOne.GetClient();
                    var userTwoClient = userTwo.GetClient();
                    if (userOneClient == null || userTwoClient == null)
                    {
                        session.SendNotification(item.GetRoom().GetLanguageManager().Require("lovelock.user_invalid"));
                        return;
                    }
                    var userOneHabbo = userOneClient?.GetHabbo();
                    var userTwoHabbo = userTwoClient?.GetHabbo();
                    if (userOneHabbo == null || userTwoHabbo == null)
                    {
                        session.SendNotification(item.GetRoom().GetLanguageManager().Require("lovelock.user_invalid"));
                        return;
                    }
                    userOne.CanWalk = false;
                    userTwo.CanWalk = false;
                    item.InteractingUser = userOneHabbo.Id;
                    item.InteractingUser2 = userTwoHabbo.Id;
                    userOneClient!.Send(new LoveLockDialogueComposer(item.Id));
                    userTwoClient!.Send(new LoveLockDialogueComposer(item.Id));
                }
            }
            else
                return;
        }
        else
            user.MoveTo(item.SquareInFront);
    }

    public void OnWiredTrigger(Item item) { }
}
