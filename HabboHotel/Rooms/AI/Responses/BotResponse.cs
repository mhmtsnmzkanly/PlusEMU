using Plus.HabboHotel.Catalog.Utilities;

namespace Plus.HabboHotel.Rooms.AI.Responses;

public class BotResponse
{
    public BotResponse(string botAi, string keywords, string responseText, string responseMode, string responseBeverages)
    {
        AiType = BotUtility.GetAiTypeFromString(botAi);
        Keywords = new();
        foreach (var keyword in keywords.Split(',')) Keywords.Add(keyword.ToLower());
        ResponseText = responseText;
        ResponseType = responseMode;
        BeverageIds = new();
        if (responseBeverages.Contains(","))
        {
            foreach (var vendingId in responseBeverages.Split(','))
            {
                if (int.TryParse(vendingId, out var beverageId))
                    BeverageIds.Add(beverageId);
            }
        }
        else if (!string.IsNullOrEmpty(responseBeverages)
                 && int.TryParse(responseBeverages, out var beverageId)
                 && beverageId > 0)
            BeverageIds.Add(beverageId);
    }

    public BotAiType AiType { get; set; }
    public List<string> Keywords { get; set; }
    public string ResponseText { get; set; }
    public string ResponseType { get; set; }
    public List<int> BeverageIds { get; }

    public bool KeywordMatched(string message)
    {
        foreach (var keyword in Keywords)
        {
            if (message.ToLower().Contains(keyword.ToLower()))
                return true;
        }
        return false;
    }
}
