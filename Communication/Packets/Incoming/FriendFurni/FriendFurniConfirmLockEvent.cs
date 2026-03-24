using Plus.Communication.Packets.Outgoing.Rooms.Furni.LoveLocks;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

namespace Plus.Communication.Packets.Incoming.FriendFurni;

internal class FriendFurniConfirmLockEvent : IPacketEvent
{
    private readonly IDatabase _database;

    public FriendFurniConfirmLockEvent(IDatabase database)
    {
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        var pId = packet.ReadUInt();
        var isConfirmed = packet.ReadBool();
        var room = habbo.CurrentRoom;
        if (room == null)
            return Task.CompletedTask;
        var item = room.GetRoomItemHandler().GetItem(pId);
        if (item == null || item.Definition == null || item.Definition.InteractionType != InteractionType.Lovelock)
            return Task.CompletedTask;
        var userOneId = item.InteractingUser;
        var userTwoId = item.InteractingUser2;
        var userOne = room.GetRoomUserManager().GetRoomUserByHabbo(userOneId);
        var userTwo = room.GetRoomUserManager().GetRoomUserByHabbo(userTwoId);
        if (userOne == null && userTwo == null)
        {
            item.InteractingUser = 0;
            item.InteractingUser2 = 0;
            session.SendNotification("Your partner has left the room or has cancelled the love lock.");
            return Task.CompletedTask;
        }
        var userOneClient = userOne?.GetClient();
        var userTwoClient = userTwo?.GetClient();
        var userOneHabbo = userOneClient?.GetHabbo();
        var userTwoHabbo = userTwoClient?.GetHabbo();
        if (userOneClient == null || userTwoClient == null || userOneHabbo == null || userTwoHabbo == null)
        {
            item.InteractingUser = 0;
            item.InteractingUser2 = 0;
            session.SendNotification("Your partner has left the room or has cancelled the love lock.");
            return Task.CompletedTask;
        }
        if (userOne == null)
        {
            var partner = userTwo;
            if (partner == null)
                return Task.CompletedTask;
            partner.CanWalk = true;
            userTwoClient.SendNotification("Your partner has left the room or has cancelled the love lock.");
            partner.LlPartner = 0;
            item.InteractingUser = 0;
            item.InteractingUser2 = 0;
            return Task.CompletedTask;
        }
        if (userTwo == null)
        {
            var partner = userOne;
            if (partner == null)
                return Task.CompletedTask;
            partner.CanWalk = true;
            userOneClient.SendNotification("Your partner has left the room or has cancelled the love lock.");
            partner.LlPartner = 0;
            item.InteractingUser = 0;
            item.InteractingUser2 = 0;
            return Task.CompletedTask;
        }
        if (item.ExtraData.Serialize().Contains(Convert.ToChar(5).ToString()))
        {
            userTwo.CanWalk = true;
            userTwoClient.SendNotification("It appears this love lock has already been locked.");
            userTwo.LlPartner = 0;
            userOne.CanWalk = true;
            userOneClient.SendNotification("It appears this love lock has already been locked.");
            userOne.LlPartner = 0;
            item.InteractingUser = 0;
            item.InteractingUser2 = 0;
            return Task.CompletedTask;
        }
        if (!isConfirmed)
        {
            item.InteractingUser = 0;
            item.InteractingUser2 = 0;
            userOne.LlPartner = 0;
            userTwo.LlPartner = 0;
            userOne.CanWalk = true;
            userTwo.CanWalk = true;
            return Task.CompletedTask;
        }
        if (userOneId == habbo.Id)
        {
            session.Send(new LoveLockDialogueSetLockedComposer(pId));
            userOne.LlPartner = userTwoId;
        }
        else if (userTwoId == habbo.Id)
        {
            session.Send(new LoveLockDialogueSetLockedComposer(pId));
            userTwo.LlPartner = userOneId;
        }
        if (userOne.LlPartner == 0 || userTwo.LlPartner == 0)
            return Task.CompletedTask;
        item.ExtraData.Store(
            $"1{(char)5}{userOne.GetUsername()}{(char)5}{userTwo.GetUsername()}{(char)5}{userOneHabbo.Look}{(char)5}{userTwoHabbo.Look}{(char)5}{DateTime.Now:dd/MM/yyyy}");
        item.InteractingUser = 0;
        item.InteractingUser2 = 0;
        userOne.LlPartner = 0;
        userTwo.LlPartner = 0;
        item.UpdateState(true, true);
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery("UPDATE `items` SET `extra_data` = @extraData WHERE `id` = @ID LIMIT 1");
            dbClient.AddParameter("extraData", item.ExtraData);
            dbClient.AddParameter("ID", item.Id);
            dbClient.RunQuery();
        }
        userOneClient.Send(new LoveLockDialogueCloseComposer(pId));
        userTwoClient.Send(new LoveLockDialogueCloseComposer(pId));
        userOne.CanWalk = true;
        userTwo.CanWalk = true;
        userOne = null;
        userTwo = null;
        return Task.CompletedTask;
    }
}
