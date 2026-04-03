using Dapper;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms.FloorPlan;

internal class SaveFloorPlanModelEvent : RoomPacketEvent
{
    private readonly IRoomManager _roomManager;
    private readonly IDatabase _database;

    public SaveFloorPlanModelEvent(IRoomManager roomManager, IDatabase database)
    {
        _roomManager = roomManager;
        _database = database;
    }

    public override Task Parse(Room room, GameClient session, IIncomingPacket packet)
    {
        if (!room.CheckRights(session, true)) return Task.CompletedTask;
        char[] validLetters = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', '\r' };
        var map = packet.ReadString().ToLower().TrimEnd();
        if (map.Length > 4159) { session.Send(new RoomNotificationComposer("floorplan_editor.error", "errors", "(%%%general%%%): %%%too_large_area%%% (%%%max%%% 2048 %%%tiles%%%)")); return Task.CompletedTask; }
        if (map.Any(letter => !validLetters.Contains(letter)) || string.IsNullOrEmpty(map)) { session.Send(new RoomNotificationComposer("floorplan_editor.error", "errors", "Oops, it appears that you have entered an invalid floor map!")); return Task.CompletedTask; }
        var modelData = map.Split('\r');
        var sizeY = modelData.Length;
        var sizeX = modelData[0].Length;
        if (sizeY > 64 || sizeX > 64) { session.Send(new RoomNotificationComposer("floorplan_editor.error", "errors", "The maximum height and width of a model is 64x64!")); return Task.CompletedTask; }
        var lastLineLength = 0; var isValid = true;
        foreach (var data in modelData) { if (lastLineLength == 0) { lastLineLength = data.Length; continue; } if (lastLineLength != data.Length) isValid = false; }
        if (!isValid) { session.Send(new RoomNotificationComposer("floorplan_editor.error", "errors", "Oops, it appears that you have entered an invalid floor map!")); return Task.CompletedTask; }
        var doorX = packet.ReadInt(); var doorY = packet.ReadInt(); var doorDirection = packet.ReadInt();
        var wallThick = packet.ReadInt(); var floorThick = packet.ReadInt(); var wallHeight = packet.ReadInt();
        var doorZ = 0;
        if (doorY >= 0 && doorY < modelData.Length && doorX >= 0 && doorX < modelData[doorY].Length)
            doorZ = Parse(modelData[doorY][doorX]);
        if (wallThick > 1) wallThick = 1; if (wallThick < -2) wallThick = -2;
        if (floorThick > 1) floorThick = 1; if (floorThick < -2) wallThick = -2;
        if (wallHeight < 0) wallHeight = 0; if (wallHeight > 15) wallHeight = 15;
        var modelName = $"model_bc_{room.Id}";
        map += $"\r{new string('x', sizeX)}";
        var modelParams = new { ModelName = $"model_bc_{room.Id}", DoorX = doorX, DoorY = doorY, DoorZ = doorZ, DoorDirection = doorDirection, Map = map, WallHeight = wallHeight };
        using var db = _database.Connection();
        var exists = db.QueryFirstOrDefault("SELECT `id` FROM `room_models` WHERE `id` = @ModelName AND `custom` = '1' LIMIT 1", new { modelParams.ModelName });
        if (exists == null)
            db.Execute("INSERT INTO `room_models` (`id`,`door_x`,`door_y`,`door_z`,`door_dir`,`heightmap`,`custom`,`wall_height`) VALUES (@ModelName,@DoorX,@DoorY,@DoorZ,@DoorDirection,@Map,'1',@WallHeight)", modelParams);
        else
            db.Execute("UPDATE `room_models` SET `heightmap`=@Map,`door_x`=@DoorX,`door_y`=@DoorY,`door_z`=@DoorZ,`door_dir`=@DoorDirection,`wall_height`=@WallHeight WHERE `id`=@ModelName LIMIT 1", modelParams);
        db.Execute("UPDATE `rooms` SET `model_name`=@ModelName,`wallthick`=@WallThick,`floorthick`=@FloorThick WHERE `id`=@roomId LIMIT 1",
            new { ModelName = $"model_bc_{room.Id}", WallThick = wallThick, FloorThick = floorThick, roomId = room.Id });
        room.ModelName = modelName; room.WallThickness = wallThick; room.FloorThickness = floorThick;
        var usersToReturn = room.GetRoomUserManager().GetRoomUsers().ToList();
        _roomManager.ReloadModel(modelName);
        _roomManager.UnloadRoom(room.Id);
        foreach (var user in usersToReturn) { if (user == null || user.GetClient() == null) continue; user.GetClient().Send(new RoomForwardComposer(room.Id)); }
        return Task.CompletedTask;
    }

    private static short Parse(char input)
    {
        switch (input)
        {
            default: return 0;
            case '1': return 1; case '2': return 2; case '3': return 3; case '4': return 4; case '5': return 5;
            case '6': return 6; case '7': return 7; case '8': return 8; case '9': return 9;
            case 'a': return 10; case 'b': return 11; case 'c': return 12; case 'd': return 13; case 'e': return 14;
            case 'f': return 15; case 'g': return 16; case 'h': return 17; case 'i': return 18; case 'j': return 19;
            case 'k': return 20; case 'l': return 21; case 'm': return 22; case 'n': return 23; case 'o': return 24;
            case 'p': return 25; case 'q': return 26; case 'r': return 27; case 's': return 28; case 't': return 29;
            case 'u': return 30; case 'v': return 31; case 'w': return 32;
        }
    }
}
