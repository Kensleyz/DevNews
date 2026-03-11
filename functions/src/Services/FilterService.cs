using DailyDevPodcast.Functions.Models;
using Microsoft.Extensions.Logging;

namespace DailyDevPodcast.Functions.Services;

public class FilterService
{
    private readonly ILogger<FilterService> _logger;

    // Keywords that make an item relevant to the target stack
    private static readonly string[] RelevantKeywords =
    [
        ".net", "dotnet", "c#", "csharp", "asp.net",
        "azure", "microsoft",
        "react", "react native", "expo",
        "ai", "llm", "gpt", "openai", "copilot", "machine learning", "ml",
        "blazor", "maui", "entity framework", "ef core",
        "devops", "github actions", "bicep", "terraform",
        "typescript", "javascript", "node",
        "api", "rest", "graphql", "microservices",
        "docker", "kubernetes", "k8s", "container"
    ];

    // Keywords that make an item noise — skip regardless of score
    private static readonly string[] ExcludeKeywords =
    [
        "cryptocurrency", "bitcoin", "crypto", "blockchain", "nft",
        "hiring", "job posting", "who is hiring",
        "latex", "overleaf", "ijcv"
    ];

    private const int MaxItems = 25;

    public FilterService(ILogger<FilterService> logger)
    {
        _logger = logger;
    }

    public List<FeedItem> Filter(List<FeedItem> items)
    {
        var filtered = items
            .Where(IsRelevant)
            .Where(item => !IsNoise(item))
            .OrderByDescending(ScoreItem)
            .Take(MaxItems)
            .ToList();

        _logger.LogInformation("Filter: {Input} items → {Output} relevant items", items.Count, filtered.Count);
        return filtered;
    }

    private static bool IsRelevant(FeedItem item)
    {
        // Reddit subs are already filtered by topic — keep all of them
        if (item.Source.StartsWith("Reddit/")) return true;
        if (item.Source == "DevTo") return true; // DevTo tags are already stack-specific

        var text = $"{item.Title} {item.Summary}".ToLowerInvariant();
        return RelevantKeywords.Any(kw => text.Contains(kw));
    }

    private static bool IsNoise(FeedItem item)
    {
        var text = $"{item.Title} {item.Summary}".ToLowerInvariant();
        return ExcludeKeywords.Any(kw => text.Contains(kw));
    }

    private static int ScoreItem(FeedItem item)
    {
        // Boost score by source quality — HN and Reddit scores are real engagement signals
        // GitHub stars are much larger numbers so we normalise them down
        return item.Source switch
        {
            "HackerNews" => item.Score * 3,
            "GitHub" => item.Score / 10,
            "DevTo" => item.Score * 5,
            _ when item.Source.StartsWith("Reddit/") => item.Score * 4,
            _ => item.Score
        };
    }
}
