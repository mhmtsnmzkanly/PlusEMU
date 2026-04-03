using Dapper;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using System.Collections.Generic;
using System.Linq;
using Plus.Core;

namespace Plus.HabboHotel.Rooms;

public class RoomFactory : IRoomFactory
{
    private sealed class RoomFactoryRow
    {
        public uint Id { get; init; }
        public string Caption { get; init; } = string.Empty;
        public string ModelName { get; init; } = string.Empty;
        public string Username { get; init; } = "Habboon";
        public int Owner { get; init; }
        public string Password { get; init; } = string.Empty;
        public int Score { get; init; }
        public string RoomType { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public int UsersNow { get; init; }
        public int UsersMax { get; init; }
        public int Category { get; init; }
        public string Description { get; init; } = string.Empty;
        public string Tags { get; init; } = string.Empty;
        public string Floor { get; init; } = string.Empty;
        public string Landscape { get; init; } = string.Empty;
        public string AllowPets { get; init; } = "0";
        public string AllowPetsEat { get; init; } = "0";
        public string RoomBlockingDisabled { get; init; } = "0";
        public string AllowHidewall { get; init; } = "0";
        public int WallThick { get; init; }
        public int FloorThick { get; init; }
        public string Wallpaper { get; init; } = string.Empty;
        public int MuteSettings { get; init; }
        public int BanSettings { get; init; }
        public int KickSettings { get; init; }
        public int ChatMode { get; init; }
        public int ChatSize { get; init; }
        public int ChatSpeed { get; init; }
        public int ChatExtraFlood { get; init; }
        public int ChatHearingDistance { get; init; }
        public int TradeSettings { get; init; }
        public string PushEnabled { get; init; } = "0";
        public string PullEnabled { get; init; } = "0";
        public string SpushEnabled { get; init; } = "0";
        public string SpullEnabled { get; init; } = "0";
        public string EnablesEnabled { get; init; } = "0";
        public string RespectNotificationsEnabled { get; init; } = "0";
        public string PetMorphsAllowed { get; init; } = "0";
        public int GroupId { get; init; }
        public int SalePrice { get; init; }
        public string LayEnabled { get; init; } = "0";
    }

    private readonly IDatabase _database;
    private readonly IRoomDependencyResolver _roomDependencyResolver;

    public RoomFactory(IDatabase database, IRoomDependencyResolver roomDependencyResolver)
    {
        BootProbe.Write("Entering RoomFactory constructor...");
        _database = database;
        _roomDependencyResolver = roomDependencyResolver;
        BootProbe.Write("Leaving RoomFactory constructor.");
    }

    public List<RoomData> GetRoomsDataByOwnerSortByName(int ownerId)
    {
        var data = new List<RoomData>();
        var rooms = GetRoomRowsByOwner(ownerId);

        foreach (var room in rooms)
            data.Add(ResolveRoomData(room));

        return data;
    }

    public bool TryGetData(uint roomId, out RoomData? data)
    {
        if (!TryGetRoomRow(roomId, out var room))
        {
            data = null;
            return false;
        }

        data = Map(room!);
        return true;
    }

    public RoomData CreateRoomData(GameClient session, string name, string description, int category, int maxVisitors, int tradeSettings, RoomModel model, string wallpaper, string floor, string landscape, int wallthick, int floorthick)
    {
        using var connection = _database.Connection();
        var id = connection.ExecuteScalar<uint>(
            """
            INSERT INTO `rooms` (`caption`, `description`, `category`, `users_max`, `trade_settings`, `model_name`, `owner`, `wallpaper`, `floor`, `landscape`, `wallthick`, `floorthick`)
            VALUES (@name, @description, @category, @maxVisitors, @tradeSettings, @modelName, @ownerId, @wallpaper, @floor, @landscape, @wallthick, @floorthick);
            SELECT LAST_INSERT_ID();
            """,
            new
            {
                name,
                description,
                category,
                maxVisitors,
                tradeSettings,
                modelName = model.Id,
                ownerId = session.GetHabbo().Id,
                wallpaper,
                floor,
                landscape,
                wallthick,
                floorthick
            });

        return new RoomData(id, name, model.Id, session.GetHabbo().Username, session.GetHabbo().Id, "", 0, "private", "open", 0, maxVisitors, category, description, "", floor, landscape,
            true, true, false, false, wallthick, floorthick, wallpaper, 0, 0, 0, 0, 1, 1, 0, 14, tradeSettings, true, true, true, true, true, true, true, 0, 0, true, model);
    }

    private IEnumerable<RoomFactoryRow> GetRoomRowsByOwner(int ownerId)
    {
        using var connection = _database.Connection();
        return connection.Query<RoomFactoryRow>(
            $"""
            {GetRoomProjectionSql()}
            WHERE `owner` = @ownerId
            ORDER BY `caption` ASC
            """,
            new { ownerId }).ToList();
    }

    private bool TryGetRoomRow(uint roomId, out RoomFactoryRow? row)
    {
        using var connection = _database.Connection();
        row = connection.QuerySingleOrDefault<RoomFactoryRow>(
            $"""
            {GetRoomProjectionSql()}
            WHERE `rooms`.`id` = @roomId
            LIMIT 1
            """,
            new { roomId });
        return row != null;
    }

    private RoomData ResolveRoomData(RoomFactoryRow row)
    {
        if (_roomDependencyResolver.GetRoomManager().TryGetRoom(row.Id, out var roomInstance) && roomInstance != null)
            return roomInstance.Data;

        return Map(row);
    }

    private static string GetRoomProjectionSql()
    {
        return
            """
            SELECT
                `rooms`.`id` AS Id,
                `rooms`.`caption` AS Caption,
                `rooms`.`model_name` AS ModelName,
                `users`.`username` AS Username,
                `rooms`.`owner` AS Owner,
                `rooms`.`password` AS Password,
                `rooms`.`score` AS Score,
                `rooms`.`roomtype` AS RoomType,
                `rooms`.`state` AS State,
                `rooms`.`users_now` AS UsersNow,
                `rooms`.`users_max` AS UsersMax,
                `rooms`.`category` AS Category,
                `rooms`.`description` AS Description,
                `rooms`.`tags` AS Tags,
                `rooms`.`floor` AS Floor,
                `rooms`.`landscape` AS Landscape,
                `rooms`.`allow_pets` AS AllowPets,
                `rooms`.`allow_pets_eat` AS AllowPetsEat,
                `rooms`.`room_blocking_disabled` AS RoomBlockingDisabled,
                `rooms`.`allow_hidewall` AS AllowHidewall,
                `rooms`.`wallthick` AS WallThick,
                `rooms`.`floorthick` AS FloorThick,
                `rooms`.`wallpaper` AS Wallpaper,
                `rooms`.`mute_settings` AS MuteSettings,
                `rooms`.`ban_settings` AS BanSettings,
                `rooms`.`kick_settings` AS KickSettings,
                `rooms`.`chat_mode` AS ChatMode,
                `rooms`.`chat_size` AS ChatSize,
                `rooms`.`chat_speed` AS ChatSpeed,
                `rooms`.`chat_extra_flood` AS ChatExtraFlood,
                `rooms`.`chat_hearing_distance` AS ChatHearingDistance,
                `rooms`.`trade_settings` AS TradeSettings,
                `rooms`.`push_enabled` AS PushEnabled,
                `rooms`.`pull_enabled` AS PullEnabled,
                `rooms`.`spush_enabled` AS SpushEnabled,
                `rooms`.`spull_enabled` AS SpullEnabled,
                `rooms`.`enables_enabled` AS EnablesEnabled,
                `rooms`.`respect_notifications_enabled` AS RespectNotificationsEnabled,
                `rooms`.`pet_morphs_allowed` AS PetMorphsAllowed,
                `rooms`.`group_id` AS GroupId,
                `rooms`.`sale_price` AS SalePrice,
                `rooms`.`lay_enabled` AS LayEnabled
            FROM `rooms`
            JOIN `users` ON `rooms`.`owner` = `users`.`id`
            """;
    }

    private RoomData Map(RoomFactoryRow row)
    {
        if (!_roomDependencyResolver.GetRoomManager().TryGetModel(row.ModelName, out var model) || model == null)
            throw new InvalidOperationException($"Room model '{row.ModelName}' could not be resolved for room {row.Id}.");
        var data = new RoomData(row.Id, row.Caption, row.ModelName, row.Username, row.Owner, row.Password, row.Score, row.RoomType, row.State, row.UsersNow, row.UsersMax, row.Category, row.Description,
            row.Tags, row.Floor, row.Landscape, row.AllowPets == "1", row.AllowPetsEat == "1", row.RoomBlockingDisabled == "1", row.AllowHidewall == "1", row.WallThick, row.FloorThick, row.Wallpaper,
            row.MuteSettings, row.BanSettings, row.KickSettings, row.ChatMode, row.ChatSize, row.ChatSpeed, row.ChatExtraFlood, row.ChatHearingDistance, row.TradeSettings, row.PushEnabled == "1",
            row.PullEnabled == "1", row.SpushEnabled == "1", row.SpullEnabled == "1", row.EnablesEnabled == "1", row.RespectNotificationsEnabled == "1", row.PetMorphsAllowed == "1", row.GroupId,
            row.SalePrice, row.LayEnabled == "1", model);

        if (row.GroupId > 0)
        {
            _roomDependencyResolver.GetGroupManager().TryGetGroup(row.GroupId, out var group);
            data.Group = group;
        }

        return data;
    }
}
