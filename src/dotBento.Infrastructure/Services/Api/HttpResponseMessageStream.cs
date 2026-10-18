namespace dotBento.Infrastructure.Services.Api;

internal sealed class HttpResponseMessageStream(HttpResponseMessage response, Stream innerStream) : Stream
{
    private bool _disposed;
    private long _bytesRead;

    public override bool CanRead => !_disposed && innerStream.CanRead;
    public override bool CanSeek => !_disposed && innerStream.CanSeek;
    public override bool CanWrite => false;
    public override long Length => innerStream.Length;

    public override long Position
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return innerStream.CanSeek ? innerStream.Position : _bytesRead;
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (innerStream.CanSeek)
            {
                innerStream.Position = value;
                return;
            }

            // Discord resets attachments to zero, even for forward-only HTTP streams.
            // Before the first byte is read we are already there; never fake a rewind.
            if (value != 0 || _bytesRead != 0)
                throw new NotSupportedException("The HTTP response stream cannot be repositioned.");
        }
    }

    public override void Flush() => innerStream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) =>
        innerStream.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) =>
        TrackRead(innerStream.Read(buffer, offset, count));

    public override int Read(Span<byte> buffer) =>
        TrackRead(innerStream.Read(buffer));

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        TrackRead(await innerStream.ReadAsync(buffer, offset, count, cancellationToken));

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        TrackRead(await innerStream.ReadAsync(buffer, cancellationToken));

    private int TrackRead(int count)
    {
        _bytesRead += count;
        return count;
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        innerStream.Seek(offset, origin);

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    public override void Write(ReadOnlySpan<byte> buffer) =>
        throw new NotSupportedException();

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                innerStream.Dispose();
                response.Dispose();
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await innerStream.DisposeAsync();
            response.Dispose();
            _disposed = true;
        }

        await base.DisposeAsync();
    }
}
