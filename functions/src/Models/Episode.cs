using Azure;
using Azure.Data.Tables;

namespace DailyDevPodcast.Functions.Models;

public class Episode : ITableEntity
{
    // ITableEntity — PartitionKey = "episode", RowKey = date e.g. "2026-03-11"
    public string PartitionKey { get; set; } = "episode";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Title { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;   // Blob URL
    public int DurationSeconds { get; set; }
    public string Status { get; set; } = "pending";        // pending | ready | failed
    public DateTime GeneratedAt { get; set; }
}
