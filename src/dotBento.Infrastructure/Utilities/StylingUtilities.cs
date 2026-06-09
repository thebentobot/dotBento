using CSharpFunctionalExtensions;
using dotBento.Infrastructure.Extensions;
using SkiaSharp;
using Color = System.Drawing.Color;

namespace dotBento.Infrastructure.Utilities;

public sealed class StylingUtilities(HttpClient httpClient)
{
    internal const long MaxImageBytes = 8 * 1024 * 1024;
    private const int MaxSampleDimension = 128;

    public async Task<Discord.Color> GetDominantColorAsync(string imageUrl)
    {
        using var response = await httpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Could not fetch image. Status code: {(int)response.StatusCode}.");
        }

        if (response.Content.Headers.ContentLength > MaxImageBytes)
        {
            throw new InvalidOperationException($"Image is too large. Maximum size is {MaxImageBytes} bytes.");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        await using var stream = new MaxLengthStream(responseStream, MaxImageBytes);
        using var codec = SKCodec.Create(stream)
            ?? throw new InvalidOperationException("Could not decode image stream.");
        using var bitmap = new SKBitmap(GetSampleInfo(codec.Info));
        var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels());
        if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
        {
            throw new InvalidOperationException("Could not decode image stream.");
        }

        return CalculateDominantColor(bitmap).ColorToDiscordColor();
    }

    public async Task<Result<Discord.Color>> TryGetDominantColorAsync(string imageUrl)
    {
        try
        {
            return await GetDominantColorAsync(imageUrl);
        }
        catch (Exception e)
        {
            return Result.Failure<Discord.Color>(e.Message);
        }
    }

    internal static Color CalculateDominantColor(SKBitmap bitmap)
    {
        double r = 0, g = 0, b = 0;
        var total = 0;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                r += pixel.Red;
                g += pixel.Green;
                b += pixel.Blue;
                total++;
            }
        }

        return Color.FromArgb((int)(r / total), (int)(g / total), (int)(b / total));
    }

    private static SKImageInfo GetSampleInfo(SKImageInfo sourceInfo)
    {
        if (sourceInfo.Width <= MaxSampleDimension && sourceInfo.Height <= MaxSampleDimension)
        {
            return new SKImageInfo(sourceInfo.Width, sourceInfo.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        }

        var scale = Math.Min(
            MaxSampleDimension / (double)sourceInfo.Width,
            MaxSampleDimension / (double)sourceInfo.Height);
        var width = Math.Max(1, (int)Math.Round(sourceInfo.Width * scale));
        var height = Math.Max(1, (int)Math.Round(sourceInfo.Height * scale));

        return new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
    }

    internal sealed class MaxLengthStream(Stream innerStream, long maxBytes) : Stream
    {
        private long _totalBytesRead;

        public override bool CanRead => innerStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => _totalBytesRead;
            set => throw new NotSupportedException();
        }

        public override void Flush() => innerStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = innerStream.Read(buffer, offset, count);
            TrackBytesRead(read);
            return read;
        }

        public override int Read(Span<byte> buffer)
        {
            var read = innerStream.Read(buffer);
            TrackBytesRead(read);
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            TrackBytesRead(read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await innerStream.ReadAsync(buffer, cancellationToken);
            TrackBytesRead(read);
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override void Write(ReadOnlySpan<byte> buffer) =>
            throw new NotSupportedException();

        public override ValueTask DisposeAsync() =>
            innerStream.DisposeAsync();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                innerStream.Dispose();
            }

            base.Dispose(disposing);
        }

        private void TrackBytesRead(int bytesRead)
        {
            _totalBytesRead += bytesRead;
            if (_totalBytesRead > maxBytes)
            {
                throw new InvalidOperationException($"Image is too large. Maximum size is {maxBytes} bytes.");
            }
        }
    }
}
