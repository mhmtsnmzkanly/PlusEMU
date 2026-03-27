using Plus.Database;
using Dapper;

namespace Plus.HabboHotel.Items.Data.Toner;

public class TonerData
{
    public int Enabled;
    public int Hue;
    public uint ItemId;
    public int Lightness;
    public int Saturation;

    public TonerData(uint item)
    {
        ItemId = item;
        using var db = database.Connection();
        dynamic? row = db.QueryFirstOrDefault(
            "SELECT `enabled`, `data1`, `data2`, `data3` FROM `room_items_toner` WHERE `id` = @id LIMIT 1",
            new { id = ItemId });
        if (row == null)
        {
            db.Execute(
                "INSERT INTO `room_items_toner` (`id`, `enabled`, `data1`, `data2`, `data3`) VALUES (@id, '0', 0, 0, 0)",
                new { id = ItemId });
            row = db.QueryFirstOrDefault(
                "SELECT `enabled`, `data1`, `data2`, `data3` FROM `room_items_toner` WHERE `id` = @id LIMIT 1",
                new { id = ItemId });
        }
        if (row == null)
            return;

        Enabled = int.Parse(((string?)row.enabled) ?? "0");
        Hue = (int)row.data1;
        Saturation = (int)row.data2;
        Lightness = (int)row.data3;
    }
}
