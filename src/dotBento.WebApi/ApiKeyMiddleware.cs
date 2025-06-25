using Microsoft.Extensions.Caching.Memory;

namespace dotBento.WebApi;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private const string ApiKeyHeaderName = "X-API-KEY";
    private const int MaxFailedAttempts = 3;
    private static readonly TimeSpan BlockDuration = TimeSpan.FromHours(1);

    public ApiKeyMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (_cache.TryGetValue($"Blocked_{ipAddress}", out _))
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Too many failed attempts. Try again later.");
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            await HandleFailure(ipAddress, context, "API Key was not provided");
            return;
        }

        var apiKey = configuration["ApiKey"];
        if (apiKey is null || !apiKey.Equals(extractedApiKey))
        {
            await HandleFailure(ipAddress, context, "Unauthorized access");
            return;
        }

        _cache.Remove($"Failures_{ipAddress}");
        await _next(context);
    }

    private async Task HandleFailure(string ipAddress, HttpContext context, string message)
    {
        var key = $"Failures_{ipAddress}";
        int failures = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return 0;
        });

        failures++;
        _cache.Set(key, failures, TimeSpan.FromMinutes(10));

        if (failures >= MaxFailedAttempts)
        {
            _cache.Set($"Blocked_{ipAddress}", true, BlockDuration);
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Too many failed attempts. You are temporarily blocked.");
        }
        else
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync(message);
        }
    }
}
