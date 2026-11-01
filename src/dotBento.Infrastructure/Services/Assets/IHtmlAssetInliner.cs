namespace dotBento.Infrastructure.Services.Assets;

/// <summary>
/// Rewrites a generated HTML document so every external image is an inline
/// data URI. The renderer runs without network access, so this is what keeps
/// avatars, album art, and custom backgrounds visible.
/// </summary>
public interface IHtmlAssetInliner
{
    Task<string> InlineAsync(string html, CancellationToken cancellationToken = default);
}
