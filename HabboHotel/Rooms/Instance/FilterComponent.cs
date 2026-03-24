using System.Text.RegularExpressions;

namespace Plus.HabboHotel.Rooms.Instance;

public class FilterComponent
{
    private Room? _instance;

    public FilterComponent(Room instance)
    {
        _instance = instance;
    }

    public bool AddFilter(string word)
    {
        if (_instance == null || _instance.WordFilterList.Contains(word))
            return false;
        using (var dbClient = PlusEnvironment.DatabaseManager.GetQueryReactor())
        {
            dbClient.SetQuery("INSERT INTO `room_filter` (`room_id`,`word`) VALUES(@rid,@word);");
            dbClient.AddParameter("rid", _instance.Id);
            dbClient.AddParameter("word", word);
            dbClient.RunQuery();
        }
        _instance.WordFilterList.Add(word);
        return true;
    }

    public bool RemoveFilter(string word)
    {
        if (_instance == null || !_instance.WordFilterList.Contains(word))
            return false;
        using (var dbClient = PlusEnvironment.DatabaseManager.GetQueryReactor())
        {
            dbClient.SetQuery("DELETE FROM `room_filter` WHERE `room_id` = @rid AND `word` = @word;");
            dbClient.AddParameter("rid", _instance.Id);
            dbClient.AddParameter("word", word);
            dbClient.RunQuery();
        }
        _instance.WordFilterList.Remove(word);
        return true;
    }

    public string CheckMessage(string message)
    {
        if (_instance == null)
            return message.TrimEnd(' ');

        foreach (var filter in _instance.WordFilterList)
        {
            if (message.ToLower().Contains(filter) || message == filter)
                message = Regex.Replace(message, filter, "Bobba", RegexOptions.IgnoreCase);
            else
                continue;
        }
        return message.TrimEnd(' ');
    }

    public void Cleanup()
    {
        _instance = null;
    }
}
