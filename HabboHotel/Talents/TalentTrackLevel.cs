namespace Plus.HabboHotel.Talents;

public class TalentTrackLevel
{
    private readonly Dictionary<int, TalentTrackSubLevel> _subLevels;

    public TalentTrackLevel(string type, int level, string dataActions, string dataGifts, IEnumerable<TalentTrackSubLevel> subLevels)
    {
        Type = type;
        Level = level;
        Actions = new();
        Gifts = new();
        foreach (var str in dataActions.Split('|'))
            Actions.Add(str);
        foreach (var str in dataGifts.Split('|'))
            Gifts.Add(str);
        _subLevels = subLevels.ToDictionary(s => s.Level);
    }

    public string Type { get; set; }
    public int Level { get; set; }

    public List<string> Actions { get; }

    public List<string> Gifts { get; }

    public ICollection<TalentTrackSubLevel> GetSubLevels() => _subLevels.Values;
}
