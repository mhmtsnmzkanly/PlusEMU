using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Plus.Communication.Packets.Outgoing.Camera;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Core.Settings;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Camera;

internal sealed class CameraService : ICameraService
{
    private readonly ISettingsManager _settingsManager;
    private readonly IItemDataManager _itemDataManager;
    private readonly IItemFactory _itemFactory;
    private readonly IDatabase _database;
    private readonly ILogger<CameraService> _logger;

    public CameraService(
        ISettingsManager settingsManager,
        IItemDataManager itemDataManager,
        IItemFactory itemFactory,
        IDatabase database,
        ILogger<CameraService> logger)
    {
        _settingsManager = settingsManager;
        _itemDataManager = itemDataManager;
        _itemFactory = itemFactory;
        _database = database;
        _logger = logger;
    }

    public Task SendConfiguration(GameClient session)
    {
        if (!_settingsManager.GetBoolOrDefault("camera.enabled", true))
            return Task.CompletedTask;

        session.Send(new CameraPriceComposer(
            _settingsManager.GetIntOrDefault("camera.price.credits", 5),
            _settingsManager.GetIntOrDefault("camera.price.points", 5),
            _settingsManager.GetIntOrDefault("camera.price.points.publish", 5)));

        return Task.CompletedTask;
    }

    public Task RenderRoom(GameClient session, bool thumbnail)
    {
        if (!_settingsManager.GetBoolOrDefault("camera.enabled", true))
            return Task.CompletedTask;

        if (session.GetHabbo() is not { } habbo || !habbo.TryGetCurrentRoom(out var room))
            return Task.CompletedTask;

        var timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var baseUrl = _settingsManager.GetStringOrDefault("camera.url.base", "camera/");
        if (!baseUrl.EndsWith('/'))
            baseUrl += "/";

        habbo.CameraPhotoTimestamp = timestamp;
        habbo.CameraPhotoRoomId = (int)room.Id;
        habbo.CameraPhotoUrl = $"{baseUrl}{timestamp}.png";
        habbo.CameraPhotoJson = $$"""{"t":{{timestamp}},"u":"{{habbo.CameraPhotoUrl}}","m":"plus","s":1,"w":"{{habbo.CameraPhotoUrl}}"}""";

        if (thumbnail)
            session.Send(new CameraRoomThumbnailSavedComposer());
        else
            session.Send(new CameraURLComposer(habbo.CameraPhotoUrl));

        return Task.CompletedTask;
    }

    public Task PurchasePhoto(GameClient session)
    {
        if (!_settingsManager.GetBoolOrDefault("camera.enabled", true))
            return Task.CompletedTask;

        if (session.GetHabbo() is not { } habbo || habbo.Inventory?.Furniture == null)
            return Task.CompletedTask;

        if (habbo.CameraPhotoTimestamp <= 0 || string.IsNullOrWhiteSpace(habbo.CameraPhotoJson))
            return Task.CompletedTask;

        var creditCost = _settingsManager.GetIntOrDefault("camera.price.credits", 5);
        var pointCost = _settingsManager.GetIntOrDefault("camera.price.points", 5);
        if (habbo.Credits < creditCost || habbo.Duckets < pointCost)
            return Task.CompletedTask;

        var itemDefinitionId = (uint)_settingsManager.GetIntOrDefault("camera.item_id", 3);
        if (!_itemDataManager.Items.TryGetValue(itemDefinitionId, out var definition))
        {
            _logger.LogWarning("Camera purchase skipped because configured camera item {ItemId} is missing.", itemDefinitionId);
            return Task.CompletedTask;
        }

        var item = _itemFactory.CreateSingleItemNullable(definition, habbo, habbo.CameraPhotoJson, habbo.CameraPhotoJson);
        var inventoryItem = item.ToInventoryItem();
        if (!habbo.Inventory.Furniture.AddItem(inventoryItem))
            return Task.CompletedTask;

        habbo.Credits -= creditCost;
        habbo.Duckets -= pointCost;

        using var connection = _database.Connection();
        connection.Execute(
            "UPDATE `users` SET `credits` = @credits, `activity_points` = @duckets WHERE `id` = @id LIMIT 1",
            new { credits = habbo.Credits, duckets = habbo.Duckets, id = habbo.Id });

        session.Send(new CameraPurchaseSuccesfullComposer());
        session.Send(new FurniListAddComposer(inventoryItem));
        session.Send(new FurniListNotificationComposer(item.Id, 1));
        session.Send(new CreditBalanceComposer(habbo.Credits));
        session.Send(new HabboActivityPointNotificationComposer(habbo.Duckets, 0));

        return Task.CompletedTask;
    }

    public async Task PublishPhoto(GameClient session)
    {
        if (!_settingsManager.GetBoolOrDefault("camera.enabled", true))
            return;

        if (session.GetHabbo() is not { } habbo)
            return;

        if (habbo.CameraPhotoTimestamp <= 0 || string.IsNullOrWhiteSpace(habbo.CameraPhotoUrl))
            return;

        var pointCost = _settingsManager.GetIntOrDefault("camera.price.points.publish", 5);
        if (habbo.Duckets < pointCost)
            return;

        var timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var cooldownDelay = _settingsManager.GetIntOrDefault("camera.publish.delay", 20);
        var cooldownLeft = Math.Max(0, cooldownDelay - (timestamp - habbo.CameraPublishTimestamp));
        var isOk = cooldownLeft == 0;

        if (isOk)
        {
            try
            {
                using var connection = _database.Connection();
                await connection.ExecuteAsync(
                    "INSERT INTO `camera_web` (`user_id`, `room_id`, `timestamp`, `url`) VALUES (@userId, @roomId, @timestamp, @url)",
                    new { userId = habbo.Id, roomId = habbo.CameraPhotoRoomId, timestamp, url = habbo.CameraPhotoUrl });
            }
            catch (MySqlException e) when (e.Message.Contains("camera_web"))
            {
                _logger.LogWarning("Skipping camera publish persistence because table camera_web is missing.");
            }

            habbo.CameraPublishTimestamp = timestamp;
            habbo.Duckets -= pointCost;

            using var updateConnection = _database.Connection();
            await updateConnection.ExecuteAsync(
                "UPDATE `users` SET `activity_points` = @duckets WHERE `id` = @id LIMIT 1",
                new { duckets = habbo.Duckets, id = habbo.Id });
        }

        session.Send(new CameraPublishWaitMessageComposer(isOk, cooldownLeft, isOk ? habbo.CameraPhotoUrl : string.Empty));
        if (isOk)
            session.Send(new HabboActivityPointNotificationComposer(habbo.Duckets, 0));
    }
}
