using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.Wired;

namespace Plus.HabboHotel.Items.Interactor;

public class InteractorGate : IFurniInteractor
{
    public void OnPlace(GameClient session, Item item) { }

    public void OnRemove(GameClient session, Item item) { }

    public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
    {
        var modes = item.Definition.Modes - 1;
        if (!hasRights)
            return;
        if (modes <= 0) item.UpdateState(false, true);
        var currentMode = 0;
        var newMode = 0;
        if (!int.TryParse(item.LegacyDataString, out currentMode)) { }
        if (currentMode <= 0)
            newMode = 1;
        else if (currentMode >= modes)
            newMode = 0;
        else
            newMode = currentMode + 1;
        if (newMode == 0)
            if (!item.GetRoom().GetGameMap().ItemCanBePlaced(item.GetX, item.GetY))
                return;
        item.LegacyDataString = newMode.ToString();
        item.UpdateState();

        var room = item.GetRoom();
        if (room != null)
        {
            room.GetGameMap().UpdateMapForItem(item);
            room.GetWired().TriggerEvent(WiredBoxType.TriggerStateChanges, session.GetHabbo(), item);
        }
    }

    public void OnWiredTrigger(Item item)
    {
        var modes = item.Definition.Modes - 1;
        if (modes <= 0) item.UpdateState(false, true);
        var currentMode = 0;
        var newMode = 0;
        if (!int.TryParse(item.LegacyDataString, out currentMode)) { }
        if (currentMode <= 0)
            newMode = 1;
        else if (currentMode >= modes)
            newMode = 0;
        else
            newMode = currentMode + 1;
        if (newMode == 0)
            if (!item.GetRoom().GetGameMap().ItemCanBePlaced(item.GetX, item.GetY))
                return;
        item.LegacyDataString = newMode.ToString();
        item.UpdateState();

        var room = item.GetRoom();
        if (room != null)
        {
            room.GetGameMap().UpdateMapForItem(item);
        }
        //Item.GetRoom().GenerateMaps();
    }
}