using System;
using System.Collections.Generic;
using Dapper;
using Plus.Database;

namespace Plus.HabboHotel.Users.Messenger;

public class SearchResultFactory : ISearchResultFactory
{
    private readonly IDatabase _database;

    public SearchResultFactory(IDatabase database)
    {
        _database = database;
    }

    public List<SearchResult> GetSearchResult(string query)
    {
        var results = new List<SearchResult>();
        using var connection = _database.Connection();
        var rows = connection.Query("SELECT `id`,`username`,`motto`,`look`,`last_online` FROM users WHERE username LIKE @query LIMIT 50", new { query = $"{query}%" });
        
        foreach (var row in rows)
        {
            results.Add(new SearchResult(
                Convert.ToInt32(row.id),
                Convert.ToString(row.username) ?? string.Empty,
                Convert.ToString(row.motto) ?? string.Empty,
                Convert.ToString(row.look) ?? string.Empty,
                Convert.ToString(row.last_online) ?? string.Empty));
        }
        return results;
    }
}
