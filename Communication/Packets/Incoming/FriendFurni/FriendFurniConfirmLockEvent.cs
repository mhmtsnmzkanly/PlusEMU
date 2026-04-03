using Dapper;
using Plus.Communication.Packets.Outgoing.Rooms.Furni.LoveLocks;
using Plus.Core.Language;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

namespace Plus.Communication.Packets.Incoming.FriendFurni;

internal class FriendFurniConfirmLockEvent : IPacketEvent
{
    private readonly IDatabase _database;
    private readonly ILanguageManager _languageManager;

    public FriendFurniConfirmLockEvent(IDatabase database, ILanguageManager languageManager)
    {
        _database = database;
        _languageManager = languageManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo)
            return Task.CompletedTask;

        var pId = packet.ReadUInt();
        var isConfirmed = packet.ReadBool();
        if (!habbo.TryGetCurrentRoom(out var room))
            return Task.CompletedTask;

        var item = room.GetRoomItemHandler().GetItem(pId);
        if (item == null || item.Definition == null || !item.Definition.IsLovelock)
            return Task.CompletedTask;
        var userOneId = item.InteractingUser;
        var userTwoId = item.InteractingUser2;
        var userOne = room.GetRoomUserManager().GetRoomUserByHabbo(userOneId);
        var userTwo = room.GetRoomUserManager().GetRoomUserByHabbo(userTwoId);
        if (userOne == null && userTwo == null)
        {
            item.InteractingUser = 0; item.InteractingUser2 = 0;
            session.SendNotification(_languageManager.Require("lovelock.partner_missing"));
            return Task.CompletedTask;
        }
        var userOneClient = userOne?.GetClient();
        var userTwoClient = userTwo?.GetClient();
        var userOneHabbo = userOneClient?.GetHabbo();
        var userTwoHabbo = userTwoClient?.GetHabbo();
        if (userOneClient == null || userTwoClient == null || userOneHabbo == null || userTwoHabbo == null)
        {
            item.InteractingUser = 0; item.InteractingUser2 = 0;
            session.SendNotification(_languageManager.Require("lovelock.partner_missing"));
            return Task.CompletedTask;
        }
        if (userOne == null) { userTwo!.CanWalk = true; userTwoClient.SendNotification(_languageManager.Require("lovelock.partner_missing")); userTwo.LlPartner = 0; item.InteractingUser = 0; item.InteractingUser2 = 0; return Task.CompletedTask; }
        if (userTwo == null) { userOne.CanWalk = true; userOneClient.SendNotification(_languageManager.Require("lovelock.partner_missing")); userOne.LlPartner = 0; item.InteractingUser = 0; item.InteractingUser2 = 0; return Task.CompletedTask; }
        if (item.ExtraData.Serialize().Contains(Convert.ToChar(5).ToString()))
        {
            userTwo.CanWalk = true; userTwoClient.SendNotification(_languageManager.Require("lovelock.already_locked")); userTwo.LlPartner = 0;
            userOne.CanWalk = true; userOneClient.SendNotification(_languageManager.Require("lovelock.already_locked")); userOne.LlPartner = 0;
            item.InteractingUser = 0; item.InteractingUser2 = 0;
            return Task.CompletedTask;
        }
        if (!isConfirmed)
        {
            item.InteractingUser = 0; item.InteractingUser2 = 0;
            userOne.LlPartner = 0; userTwo.LlPartner = 0;
            userOne.CanWalk = true; userTwo.CanWalk = true;
            return Task.CompletedTask;
        }
        if (userOneId == habbo.Id) { session.Send(new LoveLockDialogueSetLockedComposer(pId)); userOne.LlPartner = userTwoId; }
        else if (userTwoId == habbo.Id) { session.Send(new LoveLockDialogueSetLockedComposer(pId)); userTwo.LlPartner = userOneId; }
        if (userOne.LlPartner == 0 || userTwo.LlPartner == 0)
            return Task.CompletedTask;
        item.ExtraData.Store($"1{(char)5}{userOne.GetUsername()}{(char)5}{userTwo.GetUsername()}{(char)5}{userOneHabbo.Look}{(char)5}{userTwoHabbo.Look}{(char)5}{DateTime.Now:dd/MM/yyyy}");
        item.InteractingUser = 0; item.InteractingUser2 = 0;
        userOne.LlPartner = 0; userTwo.LlPartner = 0;
        item.UpdateState(true, true);
        using var db = _database.Connection();
        db.Execute("UPDATE `items` SET `extra_data` = @extraData WHERE `id` = @id LIMIT 1", new { extraData = item.ExtraData, id = item.Id });
        userOneClient.Send(new LoveLockDialogueCloseComposer(pId));
        userTwoClient.Send(new LoveLockDialogueCloseComposer(pId));
        userOne.CanWalk = true; userTwo.CanWalk = true;
        userOne = null; userTwo = null;
        return Task.CompletedTask;
    }
}
