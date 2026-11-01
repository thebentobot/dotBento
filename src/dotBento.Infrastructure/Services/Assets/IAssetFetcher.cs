namespace dotBento.Infrastructure.Services.Assets;

/// <summary>Fetches a remote image while enforcing the public-address policy.</summary>
public interface IAssetFetcher
{
    Task<FetchedAsset?> FetchAsync(Uri uri, CancellationToken cancellationToken = default);
}
