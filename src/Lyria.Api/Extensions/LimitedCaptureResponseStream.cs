namespace Lyria.Api.Extensions;

internal sealed class LimitedCaptureResponseStream : Stream
{
    private readonly Stream _innerStream;
    private readonly MemoryStream _captureBuffer;
    private readonly int _maxBytes;
    private bool _limitExceeded;

    public LimitedCaptureResponseStream(Stream innerStream, int maxBytes)
    {
        _innerStream = innerStream;
        _maxBytes = maxBytes;
        _captureBuffer = new MemoryStream();
    }

    public bool LimitExceeded => _limitExceeded;

    public string? GetCapturedContent()
    {
        if (_limitExceeded)
        {
            return null;
        }

        if (_captureBuffer.Length == 0)
        {
            return null;
        }

        _captureBuffer.Position = 0;
        using var reader = new StreamReader(_captureBuffer, leaveOpen: true);
        return reader.ReadToEnd();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
        CaptureBytes(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _innerStream.Write(buffer);
        CaptureBytes(buffer);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _innerStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
        CaptureBytes(buffer, offset, count);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _innerStream.WriteAsync(buffer, cancellationToken);
        CaptureBytes(buffer.Span);
    }

    public override void Flush() => _innerStream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) =>
        _innerStream.FlushAsync(cancellationToken);

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    public override void SetLength(long value) =>
        _innerStream.SetLength(value);

    private void CaptureBytes(byte[] buffer, int offset, int count)
    {
        if (_limitExceeded)
        {
            return;
        }

        if (_captureBuffer.Length + count > _maxBytes)
        {
            _limitExceeded = true;
            _captureBuffer.SetLength(0);
            return;
        }

        _captureBuffer.Write(buffer, offset, count);
    }

    private void CaptureBytes(ReadOnlySpan<byte> buffer)
    {
        if (_limitExceeded)
        {
            return;
        }

        if (_captureBuffer.Length + buffer.Length > _maxBytes)
        {
            _limitExceeded = true;
            _captureBuffer.SetLength(0);
            return;
        }

        _captureBuffer.Write(buffer);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _captureBuffer.Dispose();
        }

        base.Dispose(disposing);
    }
}
