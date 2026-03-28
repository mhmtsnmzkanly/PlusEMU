using Dapper;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.GameClients;

namespace Plus.HabboHotel.Items.Interactor;

internal class InteractorMannequin : IFurniInteractor
{
    public void OnPlace(GameClient session, Item item) { }

    public void OnRemove(GameClient session, Item item) { }

    public void OnTrigger(GameClient session, Item item, int request, bool hasRights)
    {
        var client = session;
        if (client == null)
            return;
        var habbo = client.GetHabbo();
        if (habbo == null)
            return;

        if (item.LegacyDataString.Contains(Convert.ToChar(5).ToString()))
        {
            var stuff = item.LegacyDataString.Split(Convert.ToChar(5));
            habbo.Gender = stuff[0].ToUpper();
            var newFig = new Dictionary<string, string>();
            newFig.Clear();
            foreach (var man in stuff[1].Split('.'))
            {
                foreach (var fig in habbo.Look.Split('.'))
                {
                    if (fig.Split('-')[0] == man.Split('-')[0])
                    {
                        if (newFig.ContainsKey(fig.Split('-')[0]) && !newFig.ContainsValue(man))
                        {
                            newFig.Remove(fig.Split('-')[0]);
                            newFig.Add(fig.Split('-')[0], man);
                        }
                        else if (!newFig.ContainsKey(fig.Split('-')[0]) && !newFig.ContainsValue(man)) newFig.Add(fig.Split('-')[0], man);
                    }
                    else
                    {
                        if (!newFig.ContainsKey(fig.Split('-')[0])) newFig.Add(fig.Split('-')[0], fig);
                    }
                }
            }
            var final = "";
            foreach (var str in newFig.Values) final += $"{str}.";
            habbo.Look = final.TrimEnd('.');
            using var db = item.GetRoom().GetDatabase().Connection();
            db.Execute(
                "UPDATE `users` SET `look` = @look, `gender` = @gender WHERE `id` = @id LIMIT 1",
                new { look = habbo.Look, gender = habbo.Gender, id = habbo.Id });
            if (!habbo.TryGetCurrentRoom(out var room))
                return;
            var roomUser = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Username);
            if (roomUser == null)
                return;
            client.Send(new UserChangeComposer(roomUser, true));
            room.SendPacket(new UserChangeComposer(roomUser, false));
        }
    }

    public void OnWiredTrigger(Item item) { }
}
