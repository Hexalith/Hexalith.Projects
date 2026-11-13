using System.Buffers;
namespace Story65.ImageMemo.Harness;
internal sealed class MutationPool(Action firstRent) : ArrayPool<byte>
{
    private readonly PoisonPool _inner = new();
    private bool _called;
    internal int Outstanding => _inner.Outstanding;
    internal int Rents => _inner.Rents;
    internal int Returns => _inner.Returns;
    public override byte[] Rent(int minimumLength)
    {
        byte[] value = _inner.Rent(minimumLength);
        if (!_called) { _called = true; firstRent(); }
        return value;
    }
    public override void Return(byte[] array, bool clearArray = false) => _inner.Return(array, clearArray);
}
