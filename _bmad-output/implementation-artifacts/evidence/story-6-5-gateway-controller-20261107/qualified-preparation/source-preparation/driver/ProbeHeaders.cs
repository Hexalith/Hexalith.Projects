namespace Story65.GatewayControllerProof;

using System.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

internal sealed class ProbeHeaders(IHeaderDictionary inner, FixtureState state, bool response) : IHeaderDictionary
{
    public StringValues this[string key]
    {
        get { state.Observe(response ? "response.header.read" : "request.header.read", key); return inner[key]; }
        set { if (response && key.Equals("Cache-Control", StringComparison.OrdinalIgnoreCase)) state.Throw("response"); state.Observe(response ? "response.header.write" : "request.header.write", key); inner[key] = value; }
    }
    public long? ContentLength { get => inner.ContentLength; set => inner.ContentLength = value; }
    public ICollection<string> Keys => inner.Keys;
    public ICollection<StringValues> Values => inner.Values;
    public int Count => inner.Count;
    public bool IsReadOnly => inner.IsReadOnly;
    public void Add(string key, StringValues value) => inner.Add(key, value);
    public void Add(KeyValuePair<string, StringValues> item) => inner.Add(item);
    public void Clear() => inner.Clear();
    public bool Contains(KeyValuePair<string, StringValues> item) => inner.Contains(item);
    public bool ContainsKey(string key) { state.Observe("header.contains", key); return inner.ContainsKey(key); }
    public void CopyTo(KeyValuePair<string, StringValues>[] array, int index) => inner.CopyTo(array, index);
    public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() => inner.GetEnumerator();
    public bool Remove(string key) => inner.Remove(key);
    public bool Remove(KeyValuePair<string, StringValues> item) => inner.Remove(item);
    public bool TryGetValue(string key, out StringValues value)
    { state.Observe("header.tryget", key); return inner.TryGetValue(key, out value); }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
