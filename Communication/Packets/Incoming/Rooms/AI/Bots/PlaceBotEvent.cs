using System.Data;
using Plus.Communication.Packets.Outgoing.Inventory.Bots;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.AI.Speech;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.Rooms.AI.Bots;

internal class PlaceBotEvent : RoomPacketEvent
{
    private readonly IDatabase _database;

    public PlaceBotEvent(IDatabase database)
    {
        _database = database;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var inventory = habbo.Inventory;
        if (!room.CheckRights(session, true))
            return Task.CompletedTask;
        var botId = packet.ReadInt();
        var x = packet.ReadInt();
        var y = packet.ReadInt();
        if (!room.GetGameMap().CanWalk(x, y, false) || !room.GetGameMap().ValidTile(x, y))
        {
            session.SendNotification("You cannot place a bot here!");
            return Task.CompletedTask;
        }
        if (inventory?.Bots == null || !inventory.Bots.Bots.TryGetValue(botId, out var bot))
            return Task.CompletedTask;
        var botCount = 0;
        foreach (var user in room.GetRoomUserManager().GetUserList().ToList())
        {
            if (user == null || user.IsPet || !user.IsBot)
                continue;
            botCount += 1;
        }
        if (botCount >= 5 && !(habbo.Permissions?.HasRight("bot_place_any_override") ?? false))
        {
            session.SendNotification("Sorry; 5 bots per room only!");
            return Task.CompletedTask;
        }

        //TODO: Hmm, maybe not????
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery("UPDATE `bots` SET `room_id` = @roomId, `x` = @CoordX, `y` = @CoordY WHERE `id` = @BotId LIMIT 1");
            dbClient.AddParameter("roomId", room.RoomId);
            dbClient.AddParameter("BotId", bot.Id);
            dbClient.AddParameter("CoordX", x);
            dbClient.AddParameter("CoordY", y);
            dbClient.RunQuery();
        }
        var botSpeechList = new List<RandomSpeech>();

        //TODO: Grab data?
        DataRow? getData;
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.SetQuery("SELECT `ai_type`,`rotation`,`walk_mode`,`automatic_chat`,`speaking_interval`,`mix_sentences`,`chat_bubble` FROM `bots` WHERE `id` = @BotId LIMIT 1");
            dbClient.AddParameter("BotId", bot.Id);
            getData = dbClient.GetRow();
            dbClient.SetQuery("SELECT `text` FROM `bots_speech` WHERE `bot_id` = @BotId");
            dbClient.AddParameter("BotId", bot.Id);
            var botSpeech = dbClient.GetTable();
            if (botSpeech != null)
                foreach (DataRow speech in botSpeech.Rows)
                    botSpeechList.Add(new(Convert.ToString(speech["text"]) ?? string.Empty, bot.Id));
        }
        if (getData == null)
            return Task.CompletedTask;
        var botUser = room.GetRoomUserManager().DeployBot(
            new(bot.Id, room.RoomId, Convert.ToString(getData["ai_type"]) ?? string.Empty, Convert.ToString(getData["walk_mode"]) ?? string.Empty, bot.Name, "", bot.Figure, x, y, 0, 4, 0, 0, 0, 0,
                ref botSpeechList, "", 0, bot.OwnerId, ConvertExtensions.EnumToBool(getData["automatic_chat"].ToString() ?? "0"), Convert.ToInt32(getData["speaking_interval"]),
                ConvertExtensions.EnumToBool(getData["mix_sentences"].ToString() ?? "0"), Convert.ToInt32(getData["chat_bubble"])), null!);
        botUser.Chat("Hello!");
        room.GetGameMap().UpdateUserMovement(new(x, y), new(x, y), botUser);
        if (!inventory.Bots.RemoveBot(botId))
        {
            Console.WriteLine($"Error whilst removing Bot: {bot.Id}");
            return Task.CompletedTask;
        }
        session.Send(new BotInventoryComposer(inventory.Bots.Bots.Values.ToList()));
        return Task.CompletedTask;
    }
}
