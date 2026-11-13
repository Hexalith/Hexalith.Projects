namespace Hexalith.PrivateDomainReceiverFixture;

/// <summary>Checks the live receiver scope whenever an actual result writes to its caller-owned response stream.</summary>
internal sealed class ProbeResponseStream(Action onWrite) : MemoryStream
{
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        onWrite();
        return base.WriteAsync(buffer, cancellationToken);
    }
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        onWrite();
        return base.WriteAsync(buffer, offset, count, cancellationToken);
    }
}
