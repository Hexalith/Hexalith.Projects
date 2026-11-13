namespace Story65.InvocationFixture;

using System.Text;

/// <summary>A fully in-memory duplex HTTP connection; no socket or network operation is performed.</summary>
internal sealed class RedirectMemoryStream : Stream
{
    private readonly MemoryStream input = new(Encoding.ASCII.GetBytes(
        "HTTP/1.1 307 Temporary Redirect\r\nLocation: /v1.0/invoke/eventstore/method/query\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"));
    private readonly MemoryStream output = new();
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => input.Read(buffer, offset, count);
    public override int Read(Span<byte> buffer) => input.Read(buffer);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default) => input.ReadAsync(buffer, token);
    public override void Write(byte[] buffer, int offset, int count) => output.Write(buffer, offset, count);
    public override void Write(ReadOnlySpan<byte> buffer) => output.Write(buffer);
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default) => output.WriteAsync(buffer, token);
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    protected override void Dispose(bool disposing)
    { if (disposing) { input.Dispose(); output.Dispose(); } base.Dispose(disposing); }
}
