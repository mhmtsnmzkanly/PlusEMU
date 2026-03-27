using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Interactor;

internal class InteractorSwitch : IFurniInteractor
{
    public void OnPlace(GameClient session, Item item) { }

    public void OnRemove(GameClient session, Item item) { }

    public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
    {
        if (session == null)
            return;
        var habbo = session.GetHabbo();
        if (habbo == null)
            return;

        var user = item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
        if (user == null)
            return;
        if (Gamemap.TilesTouching(item.GetX, item.GetY, user.X, user.Y))
        {
            var modes = item.Definition.Modes - 1;
            if (modes <= 0)
                return;
            PlusEnvironment.Game.QuestService.ProgressUserQuest(session, QuestType.FurniSwitch);
            var currentMode = 0;
            var newMode = 0;
            if (!int.TryParse(item.LegacyDataString, out currentMode)) { }
            if (currentMode <= 0)
                newMode = 1;
            else if (currentMode >= modes)
                newMode = 0;
            else
                newMode = currentMode + 1;
            item.LegacyDataString = newMode.ToString();
            item.UpdateState();
        }
        else
            user.MoveTo(item.SquareInFront);
    }

    public void OnWiredTrigger(Item item) { }
}
