using System.Net;
using System.Net.Sockets;
using dotBento.Infrastructure.Utilities;
using Serilog;

namespace dotBento.Infrastructure.Services.Assets;

/// <summary>
/// Fetches images from public HTTP(S) URLs. The connection is made to the
/// address that was validated, so a DNS answer cannot be swapped between the
/// check and the TCP connection (DNS rebinding).
/// </summary>
public sealed class PublicHttpAssetFetcher : IAssetFetcher, IDisposable
{
    private const int MaxBytes = 5 * 1024 * 1024;
    private const int MaxRedirects = 3;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/gif",
        "image/avif",
    };

    private readonly HttpClient _client;

    public PublicHttpAssetFetcher()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(5),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectCallback = ConnectToPublicAddressAsync,
        };

        _client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("dotBento-image-inliner/1.0");
        _client.DefaultRequestHeaders.Accept.ParseAdd("image/*");
    }

    public async Task<FetchedAsset?> FetchAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        for (var redirect = 0; redirect <= MaxRedirects; redirect++)
        {
            if (uri.Scheme is not ("http" or "https")) return null;

            try
            {
                using var response = await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (IsRedirect(response.StatusCode))
                {
                    var location = response.Headers.Location;
                    if (location is null) return null;

                    uri = location.IsAbsoluteUri ? location : new Uri(uri, location);
                    continue;
                }

                if (!response.IsSuccessStatusCode) return null;

                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType is null || !AllowedContentTypes.Contains(contentType)) return null;

                var declaredLength = response.Content.Headers.ContentLength;
                if (declaredLength > MaxBytes) return null;

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var buffer = new MemoryStream(declaredLength is > 0 ? (int)declaredLength.Value : 0);

                var chunk = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
                {
                    if (buffer.Length + read > MaxBytes) return null;
                    buffer.Write(chunk, 0, read);
                }

                if (buffer.Length == 0) return null;
                return new FetchedAsset(buffer.ToArray(), contentType);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "Failed to fetch image asset from {Host}", uri.Host);
                return null;
            }
        }

        return null;
    }

    private static bool IsRedirect(HttpStatusCode status) =>
        status is HttpStatusCode.MovedPermanently
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

    private static async ValueTask<Stream> ConnectToPublicAddressAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = IPAddress.TryParse(host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, cancellationToken);

        if (!PublicAddressValidator.AllPublic(addresses))
        {
            throw new HttpRequestException("Destination resolves to a non-public address.");
        }

        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
            }
        }

        throw new HttpRequestException("Could not connect to any public address.");
    }

    public void Dispose() => _client.Dispose();
}
