using System.Reflection;
internal sealed class NativeFormattingMemo
{
    private static readonly Type RuntimeType = typeof(object).GetType();
    private static readonly Type RuntimeMethod = typeof(object).GetMethod(nameof(ToString), Type.EmptyTypes)!.GetType();
    private readonly Dictionary<MethodInfo,string> _methods = new(ReferenceEqualityComparer.Instance);
    internal int Hits, Misses; internal long Bytes;
    internal string? Format(MethodInfo method)
    {
        if (method.GetType() != RuntimeMethod || method.DeclaringType?.GetType() != RuntimeType || method.ReflectedType?.GetType() != RuntimeType) return method.ToString();
        if (_methods.TryGetValue(method, out var value)) { Hits++; return value; }
        Misses++; value = method.ToString();
        if (value is not null && _methods.Count < 1024 && value.Length <= (1024 * 1024 - 256) / 2 && Bytes + 256 + value.Length * 2L <= 1024 * 1024)
        { _methods.Add(method,value); Bytes += 256 + value.Length * 2L; }
        return value;
    }

    internal bool JoinEnabled;
    private readonly Dictionary<Type,(string?[] Names,string Joined)> _joined = new(ReferenceEqualityComparer.Instance);
    internal string Join(Type implementation,IEnumerable<Type> interfaces)
    {
        Type[] types=interfaces.ToArray();
        bool eligible=implementation.GetType()==RuntimeType && types.All(type=>type.GetType()==RuntimeType);
        string?[] names=types.SelectMany(type=>type.GetMethods()).Select(method=>
        {
            if(method.GetType()!=RuntimeMethod)eligible=false;
            return method.ToString();
        }).Order(StringComparer.Ordinal).ToArray();
        if(eligible && _joined.TryGetValue(implementation,out var previous) && names.SequenceEqual(previous.Names,StringComparer.Ordinal))
        { Hits++;return previous.Joined; }
        Misses++;
        string joined=string.Join(';',names);
        long charged=256+24+names.Length*8L+32+joined.Length*2L+names.Sum(name=>name is null?0L:32+name.Length*2L);
        if(eligible && _joined.Count<32 && Bytes+charged<=1024*1024 && !_joined.ContainsKey(implementation))
        { _joined.Add(implementation,(names,joined));Bytes+=charged; }
        return joined;
    }
}
