namespace Plus.HabboHotel.LandingView.Promotions;

public class Promotion
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public int ButtonType { get; set; }
    public string ButtonLink { get; set; } = string.Empty;
    public string ImageLink { get; set; } = string.Empty;
}
