using System.Text.RegularExpressions;
using Dapper;
using Plus.Database;
using Plus.Utilities;

namespace Plus.HabboHotel.Rooms.Chat.Filter;

public sealed class WordFilterManager : IWordFilterManager
{
    private readonly IDatabase _database;
    private readonly List<WordFilter> _filteredWords;

    public WordFilterManager(IDatabase database)
    {
        _database = database;
        _filteredWords = new();
    }

    public void Init()
    {
        if (_filteredWords.Count > 0)
            _filteredWords.Clear();
        using var db = _database.Connection();
        var rows = db.Query("SELECT `word`, `replacement`, `strict`, `bannable` FROM `wordfilter`");
        foreach (var row in rows)
        {
            var strictValue = ((string?)row.strict) ?? "0";
            var bannableValue = ((string?)row.bannable) ?? "0";
            var word = ((string?)row.word) ?? string.Empty;
            var replacement = ((string?)row.replacement) ?? string.Empty;
            _filteredWords.Add(new(
                word,
                replacement,
                ConvertExtensions.EnumToBool(strictValue),
                ConvertExtensions.EnumToBool(bannableValue))
            );
        }
    }

    public string CheckMessage(string message)
    {
        foreach (var filter in _filteredWords.ToList())
        {
            if (message.ToLower().Contains(filter.Word) && filter.IsStrict || message == filter.Word)
                message = Regex.Replace(message, filter.Word, filter.Replacement, RegexOptions.IgnoreCase);
            else if (message.ToLower().Contains(filter.Word) && !filter.IsStrict || message == filter.Word)
            {
                var words = message.Split(' ');
                message = "";
                foreach (var word in words.ToList())
                {
                    if (word.ToLower() == filter.Word)
                        message += $"{filter.Replacement} ";
                    else
                        message += $"{word} ";
                }
            }
        }
        return message.TrimEnd(' ');
    }

    public bool CheckBannedWords(string message)
    {
        message = message.Replace(" ", "").Replace(".", "").Replace("_", "").ToLower();
        foreach (var filter in _filteredWords.ToList())
        {
            if (!filter.IsBannable)
                continue;
            if (message.Contains(filter.Word))
                return true;
        }
        return false;
    }

    public bool IsFiltered(string message)
    {
        foreach (var filter in _filteredWords.ToList())
        {
            if (message.Contains(filter.Word))
                return true;
        }
        return false;
    }
}
