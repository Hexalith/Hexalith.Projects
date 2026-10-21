namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections;

internal sealed class AdvisoryIndexIdentities(int count, Func<int, string> identity, Action<int>? onRead = null) : IReadOnlyList<string>
{
    public int Count => count;
    public string this[int index] => identity(index);
    public IEnumerator<string> GetEnumerator()
    {
        for (int index = 0; index < count; index++)
        {
            onRead?.Invoke(index);
            yield return identity(index);
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
