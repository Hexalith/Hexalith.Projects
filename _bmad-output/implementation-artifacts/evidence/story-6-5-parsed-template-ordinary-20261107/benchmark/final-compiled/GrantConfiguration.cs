using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
internal sealed class GrantConfiguration(IConfiguration inner) : IConfiguration
{
    internal int Reads;internal Func<int,string?>? Current;
    internal const string Key="EventStore:RouteCatalog:DevelopmentBootstrap:DeclaredInvocations";
    public string? this[string key]{get=>key==Key?Read():inner[key];set=>inner[key]=value;}
    private string? Read(){Reads++;return Current is null?inner[Key]:Current(Reads);}
    public IEnumerable<IConfigurationSection> GetChildren()=>inner.GetChildren();
    public IChangeToken GetReloadToken()=>inner.GetReloadToken();
    public IConfigurationSection GetSection(string key)=>inner.GetSection(key);
}
