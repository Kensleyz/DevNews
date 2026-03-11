using DailyDevPodcast.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Local test mode — bypasses the Functions host entirely
if (args.Contains("--test-feeds"))
{
    await TestFeedAggregator.RunAsync(args);
    return;
}

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddHttpClient()
    .AddSingleton<FeedAggregatorService>();

builder.Build().Run();
