namespace Story65.GatewayControllerProof;

using System.Collections;

internal sealed class ProbeItems(object admissionKey, FixtureState state) : IDictionary<object, object?>
{
    internal readonly Dictionary<object, object?> Inner = new();
    private string Kind(object key) => ReferenceEquals(key, admissionKey) ? "admission"
        : key is string text ? text : "other-object";
    public object? this[object key] { get { state.Observe("items.get", Kind(key)); return Inner[key]; }
        set { state.Observe("items.set", Kind(key)); Inner[key] = value; } }
    public ICollection<object> Keys => Inner.Keys;
    public ICollection<object?> Values => Inner.Values;
    public int Count => Inner.Count;
    public bool IsReadOnly => false;
    public void Add(object key, object? value) => Inner.Add(key, value);
    public void Add(KeyValuePair<object, object?> item) => ((IDictionary<object, object?>)Inner).Add(item);
    public void Clear() => Inner.Clear();
    public bool Contains(KeyValuePair<object, object?> item) => ((IDictionary<object, object?>)Inner).Contains(item);
    public bool ContainsKey(object key) { state.Observe("items.contains", Kind(key)); return Inner.ContainsKey(key); }
    public void CopyTo(KeyValuePair<object, object?>[] array, int index) => ((IDictionary<object, object?>)Inner).CopyTo(array, index);
    public IEnumerator<KeyValuePair<object, object?>> GetEnumerator() => Inner.GetEnumerator();
    public bool Remove(object key) { state.Observe("items.remove", Kind(key)); return Inner.Remove(key); }
    public bool Remove(KeyValuePair<object, object?> item) => ((IDictionary<object, object?>)Inner).Remove(item);
    public bool TryGetValue(object key, out object? value) { state.Observe("items.tryget", Kind(key)); return Inner.TryGetValue(key, out value); }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
