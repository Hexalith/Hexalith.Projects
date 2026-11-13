namespace Story65.ImageMemo.Harness;
using System.Buffers;
internal sealed class PoisonPool : ArrayPool<byte>
{
    private readonly HashSet<byte[]> borrowed = new(ReferenceEqualityComparer.Instance);
    internal int Outstanding => borrowed.Count;
    internal int Rents { get; private set; }
    internal int Returns { get; private set; }
    public override byte[] Rent(int minimumLength)
    {
        byte[] bytes = new byte[Math.Max(1, minimumLength)];
        if (!borrowed.Add(bytes)) throw new ParityFailure("pool-double-loan");
        Rents++; return bytes;
    }
    public override void Return(byte[] array, bool clearArray = false)
    {
        if (!borrowed.Remove(array)) throw new ParityFailure("pool-foreign-or-double-return");
        array.AsSpan().Fill(0xCC); Returns++;
    }
}
