using Dapper;

namespace Plus.HabboHotel.Talents;

public class TalentTrackLevel
{
    private readonly Dictionary<int, TalentTrackSubLevel> _subLevels;

    public TalentTrackLevel(string type, int level, string dataActions, string dataGifts)
    {
        Type = type;
        Level = level;
        Actions = new();
        Gifts = new();
        foreach (var str in dataActions.Split('|'))
            Actions.Add(str);
        foreach (var str in dataGifts.Split('|'))
            Gifts.Add(str);
        _subLevels = new();
        Init();
    }

    public string Type { get; set; }
    public int Level { get; set; }

    public List<string> Actions { get; }

    public List<string> Gifts { get; }

    public void Init()
    {
        using var db = PlusEnvironment.DatabaseManager.Connection();
        var rows = db.Query(
            "SELECT `sub_level`, `badge_code`, `required_progress` FROM `talents_sub_levels` WHERE `talent_level` = @talentLevel",
            new { talentLevel = Level });
        foreach (var row in rows)
        {
            _subLevels.Add(
                (int)row.sub_level,
                new(
                    (int)row.sub_level,
                    ((string?)row.badge_code) ?? string.Empty,
                    (int)row.required_progress));
        }
    }

    public ICollection<TalentTrackSubLevel> GetSubLevels() => _subLevels.Values;
}
