using System;
using System.Data;
using System.Linq;
using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User;

internal class ConvertCreditsCommand : IChatCommand
{
    private readonly IDatabase _database;
    public string Key => "convertcredits";
    public string PermissionRequired => "command_convert_credits";

    public string Parameters => "";

    public string Description => "Convert your exchangeable furniture into actual credits.";

    public ConvertCreditsCommand(IDatabase database)
    {
        _database = database;
    }

    public async Task Execute(GameClient session, Room room, string[] parameters)
    {
        var habbo = session.GetHabbo();
        var inventory = habbo?.Inventory?.Furniture;
        if (habbo == null || inventory == null)
        {
            session.SendNotification("Oops, an error occoured whilst converting your credits!");
            return;
        }

        var totalValue = 0;
        try
        {
            using var connection = _database.Connection();
            var items = connection.Query<uint>("SELECT `id` FROM `items` WHERE `user_id` = @userId AND (`room_id` = '0' OR `room_id` = '')", new { userId = habbo.Id }).ToList();
            if (items.Count == 0)
            {
                session.SendWhisper("You currently have no items in your inventory!");
                return;
            }
            foreach (var itemId in items)
            {
                var item = inventory.GetItem(itemId);
                if (item == null || !item.Definition.IsExchange)
                    continue;
                var value = item.Definition.BehaviourData;
                connection.Execute("DELETE FROM `items` WHERE `id` = @id LIMIT 1", new { id = item.Id });
                inventory.RemoveItem(item.Id);
                session.Send(new FurniListRemoveComposer(item.Id));
                totalValue += value;
                if (value > 0)
                {
                    habbo.Credits += value;
                    session.Send(new CreditBalanceComposer(habbo.Credits));
                }
            }
            if (totalValue > 0)
                session.SendNotification($"All credits have successfully been converted!\r\r(Total value: {totalValue} credits!");
            else
                session.SendNotification("It appears you don't have any exchangeable items!");
        }
        catch
        {
            session.SendNotification("Oops, an error occoured whilst converting your credits!");
        }
    }
}
