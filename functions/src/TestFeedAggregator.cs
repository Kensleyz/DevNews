using DailyDevPodcast.Functions.Services;
using Microsoft.Extensions.Logging;

internal static class TestFeedAggregator
{
    public static async Task RunAsync(string[] _)
    {
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<FeedAggregatorService>();

        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromSeconds(30);
        http.DefaultRequestHeaders.Add("User-Agent", "DailyDevPodcast/1.0");

        var service = new FeedAggregatorService(http, logger);

        Console.WriteLine("Fetching feeds...\n");
        var items = await service.FetchAllAsync();

        Console.WriteLine($"\n── Total: {items.Count} items ──\n");

        foreach (var group in items.GroupBy(i => i.Source).OrderBy(g => g.Key))
        {
            Console.WriteLine($"[{group.Key}] — {group.Count()} items");
            foreach (var item in group.Take(3))
                Console.WriteLine($"  • {item.Title} (score: {item.Score})");
        }
    }
}
