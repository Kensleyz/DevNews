namespace DailyDevPodcast.Functions.Models;

public class FeedItem
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // "HackerNews" | "Reddit" | "GitHub" | "DevTo"
    public int Score { get; set; }
    public DateTime PublishedAt { get; set; }
}
