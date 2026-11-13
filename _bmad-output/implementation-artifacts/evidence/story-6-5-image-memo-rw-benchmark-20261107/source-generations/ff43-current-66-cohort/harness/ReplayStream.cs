namespace Story65.ImageMemo.Harness;
internal sealed class ReplayStream(byte[] data, int chunk = int.MaxValue, bool seekable = true,
    long? reportedLength = null, Action? atEnd = null, int failAfter = int.MaxValue) : Stream
{
    private int position;
    private bool ended;
    internal int Consumed => position;
    internal int EofReads { get; private set; }
    public override bool CanRead => true;
    public override bool CanSeek => seekable;
    public override bool CanWrite => false;
    public override long Length => seekable ? reportedLength ?? data.Length : throw new NotSupportedException();
    public override long Position { get => position; set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
    public override int Read(Span<byte> buffer)
    {
        if (position >= failAfter) throw new IOException("synthetic-read-failure");
        if (position == data.Length)
        {
            if (!ended) { ended = true; atEnd?.Invoke(); }
            EofReads++; return 0;
        }
        int size = Math.Min(Math.Min(buffer.Length, chunk), data.Length - position);
        data.AsSpan(position, size).CopyTo(buffer); position += size; return size;
    }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
