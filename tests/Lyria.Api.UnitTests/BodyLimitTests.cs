using System.Text;
using Lyria.Api.Extensions;
using Xunit;

namespace Lyria.Api.UnitTests;

public class BodyLimitTests
{
    private const int MaxBodyBytes = 16384;

    [Fact]
    public void CaptureStream_BodyBelowLimit_IsPreserved()
    {
        using var innerStream = new MemoryStream();
        using var captureStream = new LimitedCaptureResponseStream(innerStream, MaxBodyBytes);

        byte[] data = Encoding.UTF8.GetBytes("""{"name":"test"}""");
        captureStream.Write(data, 0, data.Length);

        string? content = captureStream.GetCapturedContent();

        Assert.NotNull(content);
        Assert.Contains("test", content);
        Assert.False(captureStream.LimitExceeded);
    }

    [Fact]
    public void CaptureStream_BodyAtLimit_IsPreserved()
    {
        using var innerStream = new MemoryStream();
        using var captureStream = new LimitedCaptureResponseStream(innerStream, MaxBodyBytes);

        byte[] data = new byte[MaxBodyBytes];
        Array.Fill(data, (byte)'A');
        captureStream.Write(data, 0, data.Length);

        string? content = captureStream.GetCapturedContent();

        Assert.NotNull(content);
        Assert.Equal(MaxBodyBytes, content.Length);
        Assert.False(captureStream.LimitExceeded);
    }

    [Fact]
    public void CaptureStream_BodyAboveLimit_IsOmitted()
    {
        using var innerStream = new MemoryStream();
        using var captureStream = new LimitedCaptureResponseStream(innerStream, MaxBodyBytes);

        byte[] data = new byte[MaxBodyBytes + 1];
        Array.Fill(data, (byte)'A');
        captureStream.Write(data, 0, data.Length);

        string? content = captureStream.GetCapturedContent();

        Assert.Null(content);
        Assert.True(captureStream.LimitExceeded);
    }

    [Fact]
    public void CaptureStream_BodyAboveLimit_DoesNotRetainPartialContent()
    {
        using var innerStream = new MemoryStream();
        using var captureStream = new LimitedCaptureResponseStream(innerStream, 10);

        byte[] firstChunk = Encoding.UTF8.GetBytes("12345");
        captureStream.Write(firstChunk, 0, firstChunk.Length);

        byte[] secondChunk = Encoding.UTF8.GetBytes("678901");
        captureStream.Write(secondChunk, 0, secondChunk.Length);

        string? content = captureStream.GetCapturedContent();

        Assert.Null(content);
        Assert.True(captureStream.LimitExceeded);
    }

    [Fact]
    public void CaptureStream_BinaryContent_WritesToInnerStream()
    {
        using var innerStream = new MemoryStream();
        using var captureStream = new LimitedCaptureResponseStream(innerStream, MaxBodyBytes);

        byte[] binaryData = [0xFF, 0xD8, 0xFF, 0xE0];
        captureStream.Write(binaryData, 0, binaryData.Length);

        Assert.Equal(binaryData.Length, innerStream.Length);
    }

    [Fact]
    public void CaptureStream_MultipartContent_WritesToInnerStream()
    {
        using var innerStream = new MemoryStream();
        using var captureStream = new LimitedCaptureResponseStream(innerStream, MaxBodyBytes);

        byte[] data = Encoding.UTF8.GetBytes("--boundary\r\nContent-Disposition: form-data\r\n\r\nfiledata");
        captureStream.Write(data, 0, data.Length);

        Assert.Equal(data.Length, innerStream.Length);
    }
}
