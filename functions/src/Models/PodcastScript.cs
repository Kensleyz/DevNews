namespace DailyDevPodcast.Functions.Models;

public class PodcastScript
{
    public string Content { get; set; } = string.Empty;
    public int EstimatedDurationSeconds { get; set; }
    public List<string> Topicscovered { get; set; } = [];
}
