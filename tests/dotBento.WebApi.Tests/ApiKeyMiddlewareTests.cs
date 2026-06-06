using System.Net;
using dotBento.WebApi;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace dotBento.WebApi.Tests;

public class ApiKeyMiddlewareTests
{
    private static IConfiguration Configuration(string? apiKey = "secret") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(apiKey is null
                ? []
                : new Dictionary<string, string?> { ["ApiKey"] = apiKey })
            .Build();

    private static ApiKeyMiddleware CreateMiddleware(
        IMemoryCache cache,
        Action<HttpContext>? nextAction = null) =>
        new(context =>
        {
            nextAction?.Invoke(context);
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        }, cache);

    private static DefaultHttpContext CreateContext(string path = "/profile")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBody(HttpResponse response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InvokeAsync_MetricsPath_BypassesApiKeyCheck()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var nextCalled = false;
        var middleware = CreateMiddleware(cache, _ => nextCalled = true);
        var context = CreateContext("/metrics");

        await middleware.InvokeAsync(context, Configuration(apiKey: null));

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_MissingApiKey_Returns401()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var middleware = CreateMiddleware(cache);
        var context = CreateContext();

        await middleware.InvokeAsync(context, Configuration());

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("API Key was not provided", await ReadBody(context.Response));
    }

    [Fact]
    public async Task InvokeAsync_InvalidApiKey_Returns401()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var middleware = CreateMiddleware(cache);
        var context = CreateContext();
        context.Request.Headers["X-API-KEY"] = "wrong";

        await middleware.InvokeAsync(context, Configuration());

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("Unauthorized access", await ReadBody(context.Response));
    }

    [Fact]
    public async Task InvokeAsync_ThirdFailure_Returns429AndCachesBlock()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var middleware = CreateMiddleware(cache);

        for (var i = 0; i < 3; i++)
        {
            var context = CreateContext();
            context.Request.Headers["X-API-KEY"] = "wrong";

            await middleware.InvokeAsync(context, Configuration());

            if (i < 2)
            {
                Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
            }
            else
            {
                Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
                Assert.Equal(
                    "Too many failed attempts. You are temporarily blocked.",
                    await ReadBody(context.Response));
            }
        }

        Assert.True(cache.TryGetValue("Blocked_127.0.0.1", out bool blocked));
        Assert.True(blocked);
    }

    [Fact]
    public async Task InvokeAsync_ValidApiKey_ClearsFailuresAndInvokesNext()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("Failures_127.0.0.1", 2);
        cache.Set("Blocked_127.0.0.1", true);
        var nextCalled = false;
        var middleware = CreateMiddleware(cache, _ => nextCalled = true);
        var context = CreateContext();
        context.Request.Headers["X-API-KEY"] = "secret";

        await middleware.InvokeAsync(context, Configuration());

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.False(cache.TryGetValue("Failures_127.0.0.1", out _));
        Assert.False(cache.TryGetValue("Blocked_127.0.0.1", out _));
    }
}
