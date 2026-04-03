using Dapper;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Core.Language;
using Plus.Core.Settings;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni;

internal class CreditFurniRedeemEvent : RoomPacketEvent
{
    private readonly ILanguageManager _languageManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IDatabase _database;

    public CreditFurniRedeemEvent(ILanguageManager languageManager, ISettingsManager settingsManager, IDatabase database)
    {
        _languageManager = languageManager;
        _settingsManager = settingsManager;
        _database = database;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { Inventory.Furniture: { } furniture } habbo)
            return Task.CompletedTask;
        if (!room.CheckRights(session, true))
            return Task.CompletedTask;

        if (!_settingsManager.GetBoolOrDefault("room.item.exchangeables.enabled", false))
        {
            session.SendNotification(_languageManager.Require("room.item.exchangeables.disabled"));
            return Task.CompletedTask;
        }

        var exchange = room.GetRoomItemHandler().GetItem(packet.ReadUInt());
        if (exchange == null)
            return Task.CompletedTask;
        if (!exchange.Definition.IsExchange)
            return Task.CompletedTask;

        var value = exchange.Definition.BehaviourData;
        if (value > 0)
        {
            habbo.Credits += value;
            session.Send(new CreditBalanceComposer(habbo.Credits));
        }

        using var db = _database.Connection();
        db.Execute("DELETE FROM `items` WHERE `id` = @id LIMIT 1", new { id = exchange.Id });
        session.Send(new FurniListUpdateComposer());
        room.GetRoomItemHandler().RemoveFurniture(null, exchange.Id);
        furniture.RemoveItem(exchange.Id);
        session.Send(new FurniListRemoveComposer(exchange.Id));
        return Task.CompletedTask;
    }
}
