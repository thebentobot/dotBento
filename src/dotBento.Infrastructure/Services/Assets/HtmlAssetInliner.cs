using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Services.Assets;

/// <summary>
/// Inlines external images and removes active content from generated HTML.
/// The image renderer has no network access and runs with JavaScript disabled,
/// so this class is the other half of that security model.
/// </summary>
public sealed partial class HtmlAssetInliner : IHtmlAssetInliner
{
    private const long MaxTotalBytes = 12 * 1024 * 1024;
    private const string TransparentPixel =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan InliningTimeout = TimeSpan.FromSeconds(8);

    private static readonly HashSet<string> RemovableLinkRelations = new(StringComparer.OrdinalIgnoreCase)
    {
        "stylesheet", "preconnect", "dns-prefetch", "preload", "modulepreload", "prefetch",
    };

    private readonly IAssetFetcher _fetcher;
    private readonly IMemoryCache _cache;

    public HtmlAssetInliner(IAssetFetcher fetcher, IMemoryCache cache)
    {
        _fetcher = fetcher;
        _cache = cache;
    }

    public async Task<string> InlineAsync(string html, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(html)) return html;

        // A slow or hostile image host must not delay the whole command. Any
        // asset still outstanding when this fires gets its fallback instead.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(InliningTimeout);
        var token = timeout.Token;

        var document = await new HtmlParser().ParseDocumentAsync(html, token);
        var budget = new Budget(MaxTotalBytes);

        RemoveExternalLinks(document);
        RemoveScripts(document);
        RemoveInlineEventHandlers(document);

        foreach (var image in document.QuerySelectorAll("img[src]").OfType<IHtmlImageElement>())
        {
            var source = image.GetAttribute("src");
            if (string.IsNullOrWhiteSpace(source)) continue;

            var resolved = await ResolveAsync(source, budget, token);
            image.SetAttribute("src", resolved ?? TransparentPixel);
        }

        foreach (var image in document.QuerySelectorAll("img[srcset]").OfType<IHtmlImageElement>())
        {
            var sourceSet = image.GetAttribute("srcset");
            if (string.IsNullOrWhiteSpace(sourceSet)) continue;

            var rewritten = await RewriteSrcSetAsync(sourceSet, budget, token);
            if (rewritten is null)
            {
                image.RemoveAttribute("srcset");
            }
            else
            {
                image.SetAttribute("srcset", rewritten);
            }
        }

        foreach (var style in document.QuerySelectorAll("style").ToList())
        {
            var css = style.TextContent;
            if (string.IsNullOrEmpty(css)) continue;
            style.TextContent = await RewriteCssAsync(css, budget, token);
        }

        foreach (var element in document.QuerySelectorAll("[style]").ToList())
        {
            var css = element.GetAttribute("style");
            if (string.IsNullOrEmpty(css)) continue;
            element.SetAttribute("style", await RewriteCssAsync(css, budget, token));
        }

        return document.DocumentElement?.OuterHtml ?? html;
    }

    private async Task<string?> ResolveAsync(string url, Budget budget, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            // Data URIs and relative URLs are left alone. dotBento generates
            // absolute URLs, so this is mostly a safety valve.
            return url;
        }

        FetchedAsset? asset;
        try
        {
            asset = await GetAssetAsync(uri, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        if (asset is null || !budget.TryAdd(asset.Bytes.Length)) return null;
        return asset.ToDataUri();
    }

    private async Task<FetchedAsset?> GetAssetAsync(Uri uri, CancellationToken cancellationToken)
    {
        var key = uri.AbsoluteUri;
        if (_cache.TryGetValue<FetchedAsset>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var asset = await _fetcher.FetchAsync(uri, cancellationToken);
        if (asset is not null)
        {
            _cache.Set(key, asset, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
                Size = asset.Bytes.Length,
            });
        }

        return asset;
    }

    private async Task<string> RewriteCssAsync(string css, Budget budget, CancellationToken cancellationToken)
    {
        var matches = CssUrlPattern().Matches(css);
        if (matches.Count == 0) return css;

        var builder = new StringBuilder(css.Length);
        var position = 0;

        foreach (Match match in matches)
        {
            builder.Append(css, position, match.Index - position);

            var dataUri = await ResolveAsync(match.Groups["url"].Value, budget, cancellationToken);
            builder.Append(dataUri is null ? "none" : $"url(\"{dataUri}\")");

            position = match.Index + match.Length;
        }

        builder.Append(css, position, css.Length - position);
        return builder.ToString();
    }

    private async Task<string?> RewriteSrcSetAsync(string sourceSet, Budget budget, CancellationToken cancellationToken)
    {
        var candidates = sourceSet.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var rewritten = new List<string>(candidates.Length);

        foreach (var candidate in candidates)
        {
            var parts = candidate.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0) continue;

            var resolved = await ResolveAsync(parts[0], budget, cancellationToken);
            if (resolved is null) continue;

            rewritten.Add(parts.Length == 2 ? $"{resolved} {parts[1]}" : resolved);
        }

        return rewritten.Count == 0 ? null : string.Join(", ", rewritten);
    }

    private static void RemoveExternalLinks(IDocument document)
    {
        foreach (var link in document.QuerySelectorAll("link[href]").ToList())
        {
            var rel = link.GetAttribute("rel");
            if (rel is null ||
                !rel.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Any(RemovableLinkRelations.Contains))
            {
                continue;
            }

            var href = link.GetAttribute("href");
            if (href is not null &&
                Uri.TryCreate(href, UriKind.Absolute, out var uri) &&
                uri.Scheme is "http" or "https")
            {
                link.Remove();
            }
        }
    }

    private static void RemoveScripts(IDocument document)
    {
        foreach (var script in document.QuerySelectorAll("script").ToList())
        {
            script.Remove();
        }
    }

    private static void RemoveInlineEventHandlers(IDocument document)
    {
        foreach (var element in document.QuerySelectorAll("*"))
        {
            var handlers = element.Attributes
                .Where(attribute => attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                .Select(attribute => attribute.Name)
                .ToList();

            foreach (var handler in handlers)
            {
                element.RemoveAttribute(handler);
            }
        }
    }

    [GeneratedRegex("""url\(\s*(?<quote>['"]?)(?<url>https?://[^'")\s]+)\k<quote>\s*\)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CssUrlPattern();

    private sealed class Budget(long limit)
    {
        private long _used;

        public bool TryAdd(long amount)
        {
            if (_used + amount > limit) return false;
            _used += amount;
            return true;
        }
    }
}
