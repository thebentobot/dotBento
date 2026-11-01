using dotBento.Infrastructure.Services.Api;
using dotBento.Infrastructure.Services.Assets;
using Microsoft.Extensions.DependencyInjection;

namespace dotBento.Bot.Extensions;

public static class ImageRendererServiceCollectionExtensions
{
    /// <summary>
    /// Registers the renderer client, its asset inliner, and the resilience
    /// pipeline used for image renders.
    /// </summary>
    public static IServiceCollection AddImageRenderer(this IServiceCollection services)
    {
        services.AddSingleton<IAssetFetcher, PublicHttpAssetFetcher>();
        services.AddSingleton<IHtmlAssetInliner, HtmlAssetInliner>();

        services.AddHttpClient<ImageRendererService>(client =>
            {
                // The resilience pipeline owns the timeout; HttpClient's own
                // timeout would race it and produce less useful errors.
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(12);

                // The standard handler requires MaxRetryAttempts >= 1. Keep the
                // strategy present but reject every retry, so each render is
                // attempted exactly once.
                options.Retry.MaxRetryAttempts = 1;
                options.Retry.ShouldHandle = _ => ValueTask.FromResult(false);

                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            });

        return services;
    }
}
