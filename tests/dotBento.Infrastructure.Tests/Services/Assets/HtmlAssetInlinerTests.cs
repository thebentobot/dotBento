using dotBento.Infrastructure.Services.Assets;

namespace dotBento.Infrastructure.Tests.Services.Assets;

public sealed class HtmlAssetInlinerTests
{
    [Fact]
    public async Task InlineAsync_ReplacesImagesAndCssUrlsAndRemovesActiveContent()
    {
        var fetcher = new FakeFetcher();
        fetcher.Assets["https://cdn.example/avatar.png"] = new FetchedAsset([1, 2, 3], "image/png");
        fetcher.Assets["https://cdn.example/background.jpg"] = new FetchedAsset([4, 5, 6], "image/jpeg");

        using var inliner = new HtmlAssetInliner(fetcher);
        const string html = """
                            <html>
                              <head>
                                <link rel="preconnect" href="https://fonts.gstatic.com">
                                <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Urbanist">
                                <style>.bg { background-image: url('https://cdn.example/background.jpg'); }</style>
                              </head>
                              <body onload="alert('x')">
                                <script>alert('x')</script>
                                <img src="https://cdn.example/avatar.png">
                              </body>
                            </html>
                            """;

        var result = await inliner.InlineAsync(html, TestContext.Current.CancellationToken);

        Assert.DoesNotContain("fonts.googleapis.com", result);
        Assert.DoesNotContain("fonts.gstatic.com", result);
        Assert.DoesNotContain("<script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onload", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data:image/png;base64,AQID", result);
        Assert.Contains("data:image/jpeg;base64,BAUG", result);
    }

    [Fact]
    public async Task InlineAsync_UsesTransparentPixelWhenImageFetchFails()
    {
        using var inliner = new HtmlAssetInliner(new FakeFetcher());

        var result = await inliner.InlineAsync(
            """<img src="https://cdn.example/missing.png">""",
            TestContext.Current.CancellationToken);

        Assert.Contains("iVBORw0KGgo", result);
    }

    [Fact]
    public async Task InlineAsync_ReplacesFailedCssBackgroundWithNone()
    {
        using var inliner = new HtmlAssetInliner(new FakeFetcher());

        var result = await inliner.InlineAsync(
            """<style>.bg { background-image: url('https://cdn.example/missing.png'); }</style>""",
            TestContext.Current.CancellationToken);

        Assert.Contains("none", result);
        Assert.DoesNotContain("https://cdn.example/missing.png", result);
    }

    [Fact]
    public async Task InlineAsync_CachesFetchedAssets()
    {
        var fetcher = new FakeFetcher();
        fetcher.Assets["https://cdn.example/avatar.png"] = new FetchedAsset([1, 2, 3], "image/png");
        using var inliner = new HtmlAssetInliner(fetcher);

        await inliner.InlineAsync("""<img src="https://cdn.example/avatar.png">""",
            TestContext.Current.CancellationToken);
        await inliner.InlineAsync("""<img src="https://cdn.example/avatar.png">""",
            TestContext.Current.CancellationToken);

        Assert.Equal(1, fetcher.Calls);
    }

    [Fact]
    public async Task InlineAsync_LeavesDataUrisAlone()
    {
        using var inliner = new HtmlAssetInliner(new FakeFetcher());

        var result = await inliner.InlineAsync(
            """<img src="data:image/png;base64,AAAA">""",
            TestContext.Current.CancellationToken);

        Assert.Contains("data:image/png;base64,AAAA", result);
    }

    private sealed class FakeFetcher : IAssetFetcher
    {
        public Dictionary<string, FetchedAsset> Assets { get; } = new(StringComparer.Ordinal);

        public int Calls { get; private set; }

        public Task<FetchedAsset?> FetchAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Assets.TryGetValue(uri.AbsoluteUri, out var asset) ? asset : null);
        }
    }
}
