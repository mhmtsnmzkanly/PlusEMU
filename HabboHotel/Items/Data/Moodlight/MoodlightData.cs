using Plus.Database;
﻿using Dapper;
using System.Text;
using Plus.Utilities;

namespace Plus.HabboHotel.Items.Data.Moodlight;

public class MoodlightData
{
    private sealed class MoodlightRow
    {
        public string Enabled { get; init; } = "0";
        public int CurrentPreset { get; init; }
        public string PresetOne { get; init; } = "#000000,255,0";
        public string PresetTwo { get; init; } = "#000000,255,0";
        public string PresetThree { get; init; } = "#000000,255,0";
    }

    public int CurrentPreset;
    public bool Enabled;
    public uint ItemId;

    public List<MoodlightPreset> Presets = [];

    public MoodlightData(uint itemId, IDatabase database)
    {
        ItemId = itemId;
        MoodlightRow? row;
        using (var connection = database.Connection())
        {
            row = connection.QueryFirstOrDefault<MoodlightRow>(
                """
                SELECT
                    `enabled` AS Enabled,
                    `current_preset` AS CurrentPreset,
                    `preset_one` AS PresetOne,
                    `preset_two` AS PresetTwo,
                    `preset_three` AS PresetThree
                FROM `room_items_moodlight`
                WHERE `item_id` = @itemId
                LIMIT 1
                """,
                new { itemId });
        }
        if (row == null)
        {
            using var connection = database.Connection();
            connection.Execute(
                """
                INSERT INTO `room_items_moodlight` (item_id, enabled, current_preset, preset_one, preset_two, preset_three)
                VALUES (@itemId, 0, 1, '#000000,255,0', '#000000,255,0', '#000000,255,0')
                """,
                new { itemId });
            row = connection.QueryFirstOrDefault<MoodlightRow>(
                """
                SELECT
                    `enabled` AS Enabled,
                    `current_preset` AS CurrentPreset,
                    `preset_one` AS PresetOne,
                    `preset_two` AS PresetTwo,
                    `preset_three` AS PresetThree
                FROM `room_items_moodlight`
                WHERE `item_id` = @itemId
                LIMIT 1
                """,
                new { itemId });
        }
        if (row == null)
            return;

        Enabled = ConvertExtensions.EnumToBool(row.Enabled);
        CurrentPreset = row.CurrentPreset;
        Presets = new();
        Presets.Add(GeneratePreset(row.PresetOne));
        Presets.Add(GeneratePreset(row.PresetTwo));
        Presets.Add(GeneratePreset(row.PresetThree));
    }

    public void Enable(IDatabase database)
    {
        Enabled = true;
        using var connection = database.Connection();
        connection.Execute(
            "UPDATE `room_items_moodlight` SET `enabled` = 1 WHERE `item_id` = @itemId LIMIT 1",
            new { itemId = ItemId });
    }

    public void Disable(IDatabase database)
    {
        Enabled = false;
        using var connection = database.Connection();
        connection.Execute(
            "UPDATE `room_items_moodlight` SET `enabled` = 0 WHERE `item_id` = @itemId LIMIT 1",
            new { itemId = ItemId });
    }

    public void UpdatePreset(int preset, string color, int intensity, bool bgOnly, IDatabase database, bool hax = false)
    {
        if (!IsValidColor(color) || !IsValidIntensity(intensity) && !hax) return;
        string pr;
        switch (preset)
        {
            case 3:
                pr = "three";
                break;
            case 2:
                pr = "two";
                break;
            case 1:
            default:
                pr = "one";
                break;
        }
        using (var connection = database.Connection())
        {
            connection.Execute(
                $"UPDATE `room_items_moodlight` SET `preset_{pr}` = CONCAT(@color, ',', @intensity, ',', @bgOnly) WHERE `item_id` = @itemId LIMIT 1",
                new { color, intensity, bgOnly = ConvertExtensions.ToStringEnumValue(bgOnly), itemId = ItemId });
        }
        GetPreset(preset).ColorCode = color;
        GetPreset(preset).ColorIntensity = intensity;
        GetPreset(preset).BackgroundOnly = bgOnly;
    }

    public static MoodlightPreset GeneratePreset(string data)
    {
        var bits = data.Split(',');
        if (bits.Length < 3)
            return new("#000000", 255, false);
        if (!IsValidColor(bits[0])) bits[0] = "#000000";
        return new(bits[0], int.Parse(bits[1]), ConvertExtensions.EnumToBool(bits[2]));
    }

    public MoodlightPreset GetPreset(int i)
    {
        i--;
        if (Presets[i] != null) return Presets[i];
        return new("#000000", 255, false);
    }

    public static bool IsValidColor(string colorCode)
    {
        switch (colorCode)
        {
            case "#000000":
            case "#0053F7":
            case "#EA4532":
            case "#82F349":
            case "#74F5F5":
            case "#E759DE":
            case "#F2F851":
                return true;
            default:
                return false;
        }
    }

    public static bool IsValidIntensity(int intensity)
    {
        if (intensity < 0 || intensity > 255) return false;
        return true;
    }

    public string GenerateExtraData()
    {
        var preset = GetPreset(CurrentPreset);
        var sb = new StringBuilder();
        sb.Append(Enabled ? 2 : 1);
        sb.Append(",");
        sb.Append(CurrentPreset);
        sb.Append(",");
        sb.Append(preset.BackgroundOnly ? 2 : 1);
        sb.Append(",");
        sb.Append(preset.ColorCode);
        sb.Append(",");
        sb.Append(preset.ColorIntensity);
        return sb.ToString();
    }
}
