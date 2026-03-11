using System.Text.Json;
using DailyDevPodcast.Functions.Models;
using Microsoft.Extensions.Logging;

namespace DailyDevPodcast.Functions.Services;

public class FeedAggregatorService
{
    private readonly HttpClient _http;
    private readonly ILogger<FeedAggregatorService> _logger;

    // HN Algolia API returns up to 1000 results — we take top stories from last 24h
    private const string HnTopStoriesUrl = "https://hacker-news.firebaseio.com/v0/topstories.json";
    private const string HnItemUrl = "https://hacker-news.firebaseio.com/v0/item/{0}.json";

    // Reddit JSON API — no auth needed for public subreddits
    private static readonly string[] RedditSubs = ["dotnet", "csharp", "azure", "reactnative", "MachineLearning"];

    // GitHub Trending — via Algolia GitHub search (public, no auth)
    private const string GitHubTrendingUrl = "https://api.github.com/search/repositories?q=created:>{0}&sort=stars&order=desc&per_page=20";

    // Dev.to public API
    private const string DevToUrl = "https://dev.to/api/articles?per_page=30&tag={0}";
    private static readonly string[] DevToTags = ["dotnet", "azure", "csharp", "ai", "react"];

    public FeedAggregatorService(HttpClient http, ILogger<FeedAggregatorService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<FeedItem>> FetchAllAsync()
    {
        var results = new List<FeedItem>();

        var tasks = new[]
        {
            FetchHackerNewsAsync(),
            FetchRedditAsync(),
            FetchGitHubTrendingAsync(),
            FetchDevToAsync()
        };

        var settled = await Task.WhenAll(tasks.Select(async t =>
        {
            try { return await t; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Feed fetch failed — skipping source");
                return new List<FeedItem>();
            }
        }));

        foreach (var batch in settled)
            results.AddRange(batch);

        _logger.LogInformation("Aggregated {Count} raw feed items", results.Count);
        return results;
    }

    // ── Hacker News ──────────────────────────────────────────────────────────

    private async Task<List<FeedItem>> FetchHackerNewsAsync()
    {
        var items = new List<FeedItem>();

        var idsJson = await _http.GetStringAsync(HnTopStoriesUrl);
        var ids = JsonSerializer.Deserialize<int[]>(idsJson) ?? [];

        // Fetch top 30 stories concurrently
        var fetchTasks = ids.Take(30).Select(id =>
            _http.GetStringAsync(string.Format(HnItemUrl, id)));

        var stories = await Task.WhenAll(fetchTasks);

        foreach (var json in stories)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("title", out var titleProp)) continue;

            var unixTime = root.TryGetProperty("time", out var timeProp) ? timeProp.GetInt64() : 0;
            var publishedAt = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;

            // Skip items older than 24h
            if (publishedAt < DateTime.UtcNow.AddHours(-24)) continue;

            items.Add(new FeedItem
            {
                Title = titleProp.GetString() ?? string.Empty,
                Url = root.TryGetProperty("url", out var urlProp) ? urlProp.GetString() ?? string.Empty : string.Empty,
                Summary = root.TryGetProperty("text", out var textProp) ? StripHtml(textProp.GetString() ?? string.Empty) : string.Empty,
                Source = "HackerNews",
                Score = root.TryGetProperty("score", out var scoreProp) ? scoreProp.GetInt32() : 0,
                PublishedAt = publishedAt
            });
        }

        _logger.LogInformation("HackerNews: {Count} items", items.Count);
        return items;
    }

    // ── Reddit ────────────────────────────────────────────────────────────────

    private async Task<List<FeedItem>> FetchRedditAsync()
    {
        var items = new List<FeedItem>();

        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "DailyDevPodcast/1.0");

        foreach (var sub in RedditSubs)
        {
            try
            {
                var json = await _http.GetStringAsync($"https://www.reddit.com/r/{sub}/hot.json?limit=10");
                using var doc = JsonDocument.Parse(json);

                var posts = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("children");

                foreach (var post in posts.EnumerateArray())
                {
                    var data = post.GetProperty("data");
                    if (data.TryGetProperty("stickied", out var stickied) && stickied.GetBoolean()) continue;

                    var createdUtc = data.TryGetProperty("created_utc", out var ct) ? ct.GetDouble() : 0;
                    var publishedAt = DateTimeOffset.FromUnixTimeSeconds((long)createdUtc).UtcDateTime;
                    if (publishedAt < DateTime.UtcNow.AddHours(-24)) continue;

                    items.Add(new FeedItem
                    {
                        Title = data.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty,
                        Url = data.TryGetProperty("url", out var u) ? u.GetString() ?? string.Empty : string.Empty,
                        Summary = data.TryGetProperty("selftext", out var s) ? s.GetString()?.Truncate(300) ?? string.Empty : string.Empty,
                        Source = $"Reddit/r/{sub}",
                        Score = data.TryGetProperty("score", out var sc) ? sc.GetInt32() : 0,
                        PublishedAt = publishedAt
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reddit r/{Sub} fetch failed", sub);
            }
        }

        _logger.LogInformation("Reddit: {Count} items", items.Count);
        return items;
    }

    // ── GitHub Trending ───────────────────────────────────────────────────────

    private async Task<List<FeedItem>> FetchGitHubTrendingAsync()
    {
        var items = new List<FeedItem>();

        // GitHub requires a User-Agent header
        var request = new HttpRequestMessage(HttpMethod.Get,
            string.Format(GitHubTrendingUrl, DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")));
        request.Headers.Add("User-Agent", "DailyDevPodcast/1.0");
        request.Headers.Add("Accept", "application/vnd.github+json");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var repos = doc.RootElement.GetProperty("items");

        foreach (var repo in repos.EnumerateArray())
        {
            items.Add(new FeedItem
            {
                Title = $"Trending: {repo.GetProperty("full_name").GetString()}",
                Url = repo.GetProperty("html_url").GetString() ?? string.Empty,
                Summary = repo.TryGetProperty("description", out var d) ? d.GetString() ?? string.Empty : string.Empty,
                Source = "GitHub",
                Score = repo.TryGetProperty("stargazers_count", out var s) ? s.GetInt32() : 0,
                PublishedAt = DateTime.UtcNow
            });
        }

        _logger.LogInformation("GitHub Trending: {Count} items", items.Count);
        return items;
    }

    // ── Dev.to ────────────────────────────────────────────────────────────────

    private async Task<List<FeedItem>> FetchDevToAsync()
    {
        var items = new List<FeedItem>();

        foreach (var tag in DevToTags)
        {
            try
            {
                var json = await _http.GetStringAsync(string.Format(DevToUrl, tag));
                using var doc = JsonDocument.Parse(json);

                foreach (var article in doc.RootElement.EnumerateArray())
                {
                    var publishedStr = article.TryGetProperty("published_at", out var p) ? p.GetString() : null;
                    var publishedAt = DateTime.TryParse(publishedStr, out var dt) ? dt : DateTime.MinValue;
                    if (publishedAt < DateTime.UtcNow.AddHours(-24)) continue;

                    items.Add(new FeedItem
                    {
                        Title = article.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty,
                        Url = article.TryGetProperty("url", out var u) ? u.GetString() ?? string.Empty : string.Empty,
                        Summary = article.TryGetProperty("description", out var d) ? d.GetString() ?? string.Empty : string.Empty,
                        Source = "DevTo",
                        Score = article.TryGetProperty("positive_reactions_count", out var r) ? r.GetInt32() : 0,
                        PublishedAt = publishedAt
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dev.to tag {Tag} fetch failed", tag);
            }
        }

        _logger.LogInformation("Dev.to: {Count} items", items.Count);
        return items;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
}

internal static class StringExtensions
{
    public static string Truncate(this string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
